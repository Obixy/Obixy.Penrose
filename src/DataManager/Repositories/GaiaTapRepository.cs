using DataManager.Domain;
using Newtonsoft.Json.Linq;
using System.Web;
using System.Xml.Linq;

namespace DataManager.Repositories;

public class GaiaTapRepository
{
    const string queryBase = @"
WITH proxima_centauri AS (
SELECT TOP 5000
    source_id, 
    ra, 
    dec, 
    parallax, 
    phot_g_mean_mag,
    1000.0 / parallax AS proxima_distance_pc
FROM 
    gaiadr3.gaia_source
WHERE 
    source_id = {0}
)
SELECT TOP 10
    s.source_id,
    (s.ra + (p.proxima_distance_pc - (1000.0 / s.parallax)) / 3600.0) AS adjusted_ra,
    (s.dec + (p.proxima_distance_pc - (1000.0 / s.parallax)) / 3600.0) AS adjusted_dec,
    s.phot_g_mean_mag + 5 * LOG10(ABS(1000.0/s.parallax) / ABS(1000.0/s.parallax - 1000.0/p.proxima_distance_pc)) AS adjusted_mag,
    (1.0 / ((1000.0 / s.parallax) + 1.295)) * 1000.0 AS apparent_parallax,
    (s.phot_bp_mean_mag + 5 * LOG10((1000.0 / s.parallax) + 1.295) - 5) AS bp_apparent,
    (s.phot_rp_mean_mag + 5 * LOG10((1000.0 / s.parallax) + 1.295) - 5) AS rp_apparent
FROM gaiadr3.gaia_source s, proxima_centauri p
WHERE s.parallax > 0 
    AND s.phot_g_mean_mag IS NOT NULL
    AND s.source_id != p.source_id
    AND s.phot_g_mean_mag + 5 * LOG10(ABS(1000.0/s.parallax) / ABS(1000.0/s.parallax - 1000.0/p.proxima_distance_pc)) < 6.5
ORDER BY adjusted_mag ASC
";

    // Test query
    //    const string queryBase = @"
    //WITH proxima_centauri AS (
    //    SELECT source_id, ra, dec, parallax, phot_g_mean_mag
    //    FROM gaiadr3.gaia_source
    //    WHERE source_id = {0}
    //)
    //SELECT TOP 10
    //    s.source_id,
    //    s.ra,
    //    s.dec,
    //    s.parallax,
    //    s.phot_g_mean_mag,
    //    p.parallax AS proxima_parallax,
    //    ABS(1/s.parallax - 1/p.parallax) AS dist_from_proxima_pc,
    //    s.phot_g_mean_mag + 5 * LOG10(ABS(1/s.parallax) / ABS(1/s.parallax - 1/p.parallax)) AS adjusted_mag
    //FROM gaiadr3.gaia_source s, proxima_centauri p
    //WHERE s.parallax > 0 
    //    AND s.phot_g_mean_mag IS NOT NULL
    //    AND s.source_id != p.source_id
    //    AND s.phot_g_mean_mag + 5 * LOG10(ABS(1/s.parallax) / ABS(1/s.parallax - 1/p.parallax)) < 6.5";

    private readonly HttpClient httpClient;

    public GaiaTapRepository(
        HttpClient httpClient
    )
    {
        this.httpClient = httpClient;
    }

    public async Task<string> StartGaiaQueryAsync(string sourceId, CancellationToken cancellationToken = default)
    {
        var unsafeQuery = string.Format(queryBase, sourceId);
        var encodedQuery = HttpUtility.UrlEncode(unsafeQuery);
        var response = await httpClient.PostAsync($"tap-server/tap/async?REQUEST=doQuery&LANG=ADQL&FORMAT=json&QUERY={encodedQuery}", null, cancellationToken);
        response.EnsureSuccessStatusCode();

        var stringContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var doc = XDocument.Parse(stringContent);
        XNamespace ns = "http://www.ivoa.net/xml/UWS/v1.0";
        var jobIdElement = doc.Root!.Element(ns + "jobId");

        var jobId = jobIdElement!.Value;
        var jobUrl = $"{httpClient.BaseAddress}tap-server/tap/async/{jobId}";

        var startPhaseRequestUrl = $"tap-server/tap/async/{jobIdElement.Value}/phase";
        var startPhaseRequestContent = new StringContent("PHASE=RUN");
        startPhaseRequestContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");

        var starPhaseResponse = await httpClient.PostAsync(startPhaseRequestUrl, startPhaseRequestContent, cancellationToken);
        starPhaseResponse.EnsureSuccessStatusCode();

        return jobUrl;
    }

    public async Task<IEnumerable<GaiaSource>> GetJobResults(GaiaExoplanetJob job, string jobUrl, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"{jobUrl}/results/result", cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        var jsonResult = JObject.Parse(content);

        var metadata = jsonResult["metadata"] ?? throw new InvalidOperationException("Metadata is not informed");
        var keys = metadata.Select(MetaDataName).ToList();

        var data = jsonResult["data"] ?? throw new InvalidOperationException("Data is not informed");

        var dictionaries = data
            .Select(dataValue => new GaiaSource
            {
                Id = Guid.NewGuid(),
                JobId = job.Id,
                StarData = keys.ToDictionary(
                    key => key,
                    key => dataValue[keys.IndexOf(key)]?.Value<string>() ?? string.Empty
                )
            });

        return dictionaries;
    }

    public async Task<GaiaExoplanetJob.StatusTypes> CheckJobStatus(string jobUrl, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync(jobUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        var doc = XDocument.Parse(content);
        XNamespace ns = "http://www.ivoa.net/xml/UWS/v1.0";
        var phaseElement = doc.Root!.Element(ns + "phase");

        return Enum.Parse<GaiaExoplanetJob.StatusTypes>(phaseElement!.Value);
    }

    private string MetaDataName(JToken metadataValue) => ((JObject)metadataValue).GetValue("name")?.Value<string>() ?? string.Empty;
}
