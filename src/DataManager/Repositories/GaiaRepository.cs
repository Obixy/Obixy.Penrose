using DataManager.Domain;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Web;
using System.Xml.Linq;

namespace DataManager.Repositories;

public class GaiaRepository
{
    // FULL QUERY
    const string queryBase = @"
WITH proxima_centauri AS (
    SELECT source_id, ra, dec, parallax, phot_g_mean_mag
    FROM gaiadr3.gaia_source
    WHERE source_id = {0}
)
SELECT TOP 15000
    s.source_id,
    s.ra,
    s.dec,
    s.parallax,
    s.phot_g_mean_mag,
    p.parallax AS proxima_parallax,
    ABS(1/s.parallax - 1/p.parallax) AS dist_from_proxima_pc,
    s.phot_g_mean_mag + 5 * LOG10(ABS(1/s.parallax) / ABS(1/s.parallax - 1/p.parallax)) AS adjusted_mag
FROM gaiadr3.gaia_source s, proxima_centauri p
WHERE s.parallax > 0 
    AND s.phot_g_mean_mag IS NOT NULL
    AND s.source_id != p.source_id
    AND s.phot_g_mean_mag + 5 * LOG10(ABS(1/s.parallax) / ABS(1/s.parallax - 1/p.parallax)) < 6.5
ORDER BY adjusted_mag ASC";

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

    private const string GAIA_TAP_URL = "tap-server/tap/async";

    private readonly HttpClient httpClient;
    private readonly CosmosClient cosmosClient;
    private Database? database;
    private Container? jobContainer;
    private Container? sourceContainer;
    private readonly JobsManager jobsManager;

    public GaiaRepository(
        HttpClient httpClient,
        CosmosClient cosmosClient,
        JobsManager jobsManager
    )
    {
        this.httpClient = httpClient;
        this.cosmosClient = cosmosClient;
        this.jobsManager = jobsManager;
    }

    private async Task<Container> GetJobContainer(CancellationToken cancellationToken = default)
    {
        database ??= (await cosmosClient.CreateDatabaseIfNotExistsAsync("PenroseDb", cancellationToken: cancellationToken)).Database;
        return jobContainer ??= (await database.CreateContainerIfNotExistsAsync("JobContext", "/PartitionKey", throughput: 400, cancellationToken: cancellationToken)).Container;
    }

    private async Task<Container> GetSourceContainer(CancellationToken cancellationToken = default)
    {
        database ??= (await cosmosClient.CreateDatabaseIfNotExistsAsync("PenroseDb", cancellationToken: cancellationToken)).Database;
        return sourceContainer ??= (await database.CreateContainerIfNotExistsAsync("SourceContext", "/PartitionKey", throughput: 400, cancellationToken: cancellationToken)).Container;
    }

    private const int batchUnitSize = 100;
    public async Task AddSourceBatch(IEnumerable<GaiaSource> gaiaSources, GaiaExoplanetJob exoplanetJob, CancellationToken cancellationToken = default)
    {
        int currentSkip = 0;
        var count = gaiaSources.Count();

        var sourceContainer = await GetSourceContainer(cancellationToken);
        while (currentSkip < count)
        { 
            var batchUnit = gaiaSources.Skip(currentSkip).Take(batchUnitSize).ToArray();
            var transaction = sourceContainer.CreateTransactionalBatch(new PartitionKey(exoplanetJob.PartitionKey));

            foreach (var item in batchUnit)
                transaction.CreateItem(item);

            using var batchResponse = await transaction.ExecuteAsync(cancellationToken);

            if (!batchResponse.IsSuccessStatusCode)
            {
                throw new Exception($"Batch failed with status code {batchResponse.StatusCode}");
            }

            currentSkip += batchUnitSize;
        }

        exoplanetJob.Status = GaiaExoplanetJob.StatusTypes.COMPLETED;

        var container = await GetJobContainer(cancellationToken);
        var updateResponse = await container.UpsertItemAsync(exoplanetJob, new PartitionKey(exoplanetJob.PartitionKey), cancellationToken: cancellationToken);

        if (updateResponse.StatusCode != System.Net.HttpStatusCode.OK)
        {
            throw new Exception($"Update failed with status code {updateResponse.StatusCode}");
        }

    }

    public async Task StartGaiaQueryAsync(string sourceId, CancellationToken cancellationToken = default)
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

        var starPhaseResponse = await httpClient.PostAsync(startPhaseRequestUrl, startPhaseRequestContent);
        starPhaseResponse.EnsureSuccessStatusCode();

        var jobsContainer = await GetJobContainer(cancellationToken);

        var gaiaJob = new GaiaExoplanetJob { Id = Guid.NewGuid(), JobUrl = jobUrl, SourceId = sourceId, Status = GaiaExoplanetJob.StatusTypes.PENDING };

        await jobsContainer.CreateItemAsync(
            gaiaJob,
            new PartitionKey(gaiaJob.PartitionKey),
            cancellationToken: cancellationToken
        );

        jobsManager.Add(sourceId, jobUrl);
    }

    public record JobGetterFilters(IEnumerable<GaiaExoplanetJob.StatusTypes> Status);
    public async Task<IEnumerable<GaiaExoplanetJob>> GetJobs(JobGetterFilters? jobGetterFilters = null, CancellationToken cancellationToken = default)
    {
        var container = await GetJobContainer(cancellationToken);

        var queryBuilder = new StringBuilder("SELECT * FROM gaiaJobs gj");

        if (jobGetterFilters is not null)
        {
            queryBuilder.AppendLine(" WHERE ");
            if (jobGetterFilters.Status.Any())
                queryBuilder.Append($"ARRAY_CONTAINS(@{nameof(jobGetterFilters.Status).ToLower()}, gj.Status)");
        }

        var query = new QueryDefinition(queryBuilder.ToString());

        if (jobGetterFilters is not null)
        {
            if (jobGetterFilters.Status is not null)
                query.WithParameter(
                    $"@status",
                     jobGetterFilters.Status.Select(s => (int)s).ToArray()
                );
        }

        var gaiaJobs = new HashSet<IEnumerable<GaiaExoplanetJob>>();

        using var resultSetIterator = container.GetItemQueryIterator<GaiaExoplanetJob>(query);
        while (resultSetIterator.HasMoreResults)
            gaiaJobs.Add(await resultSetIterator.ReadNextAsync(cancellationToken));

        return gaiaJobs.SelectMany(gaiajob => gaiajob);
    }

    public async Task<GaiaExoplanetJob?> GetJob(string sourceId, CancellationToken cancellationToken = default)
    {
        var container = await GetJobContainer(cancellationToken);

        var query = new QueryDefinition("SELECT TOP 1 * FROM gaiaJobs gj WHERE gj.SourceId = @sourceId")
            .WithParameter("@sourceId", sourceId);

        using var resultSetIterator = container.GetItemQueryIterator<GaiaExoplanetJob>(query);

        FeedResponse<GaiaExoplanetJob>? response = null;

        while (resultSetIterator.HasMoreResults)
            response = await resultSetIterator.ReadNextAsync(cancellationToken);

        return response?.FirstOrDefault();
    }

    public async Task<IEnumerable<GaiaSource>> GetJobResults(GaiaExoplanetJob job, string jobUrl, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"{jobUrl}/results/result", cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();

        var jsonResult = JObject.Parse(content);

        var keys = new HashSet<string>();

        var metadata = jsonResult["metadata"];

        foreach (var metadataValue in metadata)
        {
            keys.Add((metadataValue as JObject).GetValue("name").Value<string>());
        }

        var data = jsonResult["data"];

        var dictionaries = new HashSet<GaiaSource>();

        foreach (var dataValue in data)
        {
            var dictionary = new Dictionary<string, string>();
            for (int i = 0; i < keys.Count; i++)
            {
                dictionary.Add(keys.ElementAt(i), dataValue[i]!.Value<string>()!);
            }

            dictionaries.Add(new GaiaSource { 
                Id = Guid.NewGuid(),
                JobId = job.Id!.Value,
                StarData = dictionary
            });
        }

        return dictionaries;
    }

    public async Task<GaiaExoplanetJob.StatusTypes> CheckJobStatus(string jobUrl, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync(jobUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        string content = await response.Content.ReadAsStringAsync(cancellationToken);

        var doc = XDocument.Parse(content);
        XNamespace ns = "http://www.ivoa.net/xml/UWS/v1.0";
        var phaseElement = doc.Root!.Element(ns + "phase");

        return Enum.Parse<GaiaExoplanetJob.StatusTypes>(phaseElement!.Value);
    }
}
