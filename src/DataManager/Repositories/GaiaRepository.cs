using Microsoft.Azure.Cosmos;
using System.Text;
using System.Xml.Linq;
using static DataManager.Repositories.GaiaJob;

namespace DataManager.Repositories;

public record GaiaJob(string SourceId, string JobUrl, StatusTypes Status)
{
    public StatusTypes Status { get; set; } = Status;
    public enum StatusTypes
    {
        RUNNING,
        ERROR,
        COMPLETED
    }
}

public class GaiaRepository
{
    const string queryBase = @"
                                  WITH proxima_centauri AS (
                                    SELECT source_id, ra, dec, parallax, phot_g_mean_mag
                                    FROM gaiadr3.gaia_source
                                    WHERE source_id = {0}
                                  ),
                                  stars_from_proxima AS (
                                    SELECT 
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
                                  )
                                  SELECT TOP 10 *
                                  FROM stars_from_proxima
                                  WHERE adjusted_mag < 6.5  -- Limit to stars potentially visible to naked eye
                                  ORDER BY adjusted_mag ASC
                             ";

    private const string GAIA_TAP_URL = "tap-server/tap/async";

    private readonly HttpClient httpClient;
    private readonly CosmosClient cosmosClient;
    private Database? database;
    private Container? container;
    private readonly JobsManager jobsManager;

    public GaiaRepository(
        HttpClient httpClient,
        CosmosClient cosmosClient,
        Database? database,
        Container? container,
        JobsManager jobsManager
    )
    {
        this.httpClient = httpClient;
        this.cosmosClient = cosmosClient;
        this.database = database;
        this.container = container;
        this.jobsManager = jobsManager;
    }

    private async Task<Container> GetContainer(CancellationToken cancellationToken = default)
    {
        database ??= (await cosmosClient.CreateDatabaseIfNotExistsAsync("PenroseDb", cancellationToken: cancellationToken)).Database;
        return container ??= (await database.CreateContainerIfNotExistsAsync("PenroseDbContext", "/PartitionKey", throughput: 400, cancellationToken: cancellationToken)).Container;
    }

    public async Task AddJobResult<T>(T result, CancellationToken cancellationToken = default)
    {
        var resultContainer = await GetContainer(cancellationToken);

        await resultContainer.CreateItemAsync(
            result,
            new PartitionKey("nasaSpaceApps"),
            cancellationToken: cancellationToken
        );

    }

    public async Task StartGaiaQueryAsync(string sourceId, CancellationToken cancellationToken = default)
    {
        var query = string.Format(queryBase, sourceId);

        var content = new FormUrlEncodedContent(
        [
            new("REQUEST", "doQuery"),
            new("LANG", "ADQL"),
            new("FORMAT", "json"),
            new("QUERY", query)
        ]);

        var response = await httpClient.PostAsync(GAIA_TAP_URL, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var jobsContainer = await GetContainer(cancellationToken);

        var jobUrl = response.Headers.Location!.ToString();

        await jobsContainer.CreateItemAsync(
            new GaiaJob(sourceId, jobUrl, StatusTypes.RUNNING),
            new PartitionKey("nasaSpaceApps"),
            cancellationToken: cancellationToken
        );

        jobsManager.Add(sourceId, jobUrl);
    }

    public record JobGetterFilters(IEnumerable<StatusTypes> Status);
    public async Task<IEnumerable<GaiaJob>> GetJobs(JobGetterFilters? jobGetterFilters = null, CancellationToken cancellationToken = default)
    {
        var container = await GetContainer(cancellationToken);

        var queryBuilder = new StringBuilder("SELECT * FROM gaiaJobs gj");

        if (jobGetterFilters is not null)
        {
            queryBuilder.AppendLine(" WHERE ");
            if (jobGetterFilters.Status.Any())
                queryBuilder.Append($"ARRAY_CONTAINS(@{nameof(jobGetterFilters.Status).ToLower()}, gj.status)");
        }

        var query = new QueryDefinition(queryBuilder.ToString());

        if (jobGetterFilters is not null)
        {
            if (jobGetterFilters.Status is not null)
                query.WithParameter(
                    $"@status", 
                    jobGetterFilters.Status.Select(status => status.ToString())
                );
        }

        var gaiaJobs = new HashSet<IEnumerable<GaiaJob>>();

        using var resultSetIterator = container.GetItemQueryIterator<GaiaJob>(query);
        while (resultSetIterator.HasMoreResults)
            gaiaJobs.Add(await resultSetIterator.ReadNextAsync(cancellationToken));

        return gaiaJobs.SelectMany(gaiajob => gaiajob);
    }

    public async Task<GaiaJob?> GetJob(string sourceId, CancellationToken cancellationToken = default)
    {
        var container = await GetContainer(cancellationToken);

        var query = new QueryDefinition("SELECT TOP 1 * FROM gaiaJobs gj WHERE gj.sourceId = @sourceId")
            .WithParameter("@sourceId", sourceId);

        using var resultSetIterator = container.GetItemQueryIterator<GaiaJob>(query);

        var gaiaJobs = await resultSetIterator.ReadNextAsync(cancellationToken);

        return gaiaJobs.FirstOrDefault();
    }

    public async Task<Dictionary<string, object>> GetJobResults(string jobUrl, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"{jobUrl}/results/result", cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: cancellationToken))!;
    }

    public async Task<StatusTypes> CheckJobStatus(string jobUrl, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync(jobUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        string content = await response.Content.ReadAsStringAsync(cancellationToken);

        var xdoc = XDocument.Parse(content);
        return Enum.Parse<StatusTypes>(xdoc.Root!.Element("phase")!.Value);
    }
}


class GaiaAsyncQuery
{
    private static readonly HttpClient client = new HttpClient();
    private const string GAIA_TAP_URL = "http://gea.esac.esa.int/tap-server/tap/async";

    public static async Task<string> StartGaiaQueryAsync(string query)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("REQUEST", "doQuery"),
            new KeyValuePair<string, string>("LANG", "ADQL"),
            new KeyValuePair<string, string>("FORMAT", "json"),
            new KeyValuePair<string, string>("QUERY", query)
        });

        var response = await client.PostAsync(GAIA_TAP_URL, content);
        response.EnsureSuccessStatusCode();
        string jobUrl = response.Headers.Location.ToString();
        return jobUrl;
    }

    public static async Task<string> CheckJobStatus(string jobUrl)
    {
        var response = await client.GetAsync(jobUrl);
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();
        var xdoc = XDocument.Parse(content);
        return xdoc.Root.Element("phase").Value;
    }

    public static async Task<string> GetJobResults(string jobUrl)
    {
        var response = await client.GetAsync($"{jobUrl}/results/result");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    static async Task Main()
    {
        string query = "SELECT TOP 10 source_id, ra, dec FROM gaiadr3.gaia_source";
        string jobUrl = await StartGaiaQueryAsync(query);
        Console.WriteLine($"Job URL: {jobUrl}");

        string status;
        do
        {
            await Task.Delay(5000); // Wait 5 seconds before checking again
            status = await CheckJobStatus(jobUrl);
            Console.WriteLine($"Job status: {status}");
        } while (status != "COMPLETED" && status != "ERROR");

        if (status == "COMPLETED")
        {
            string result = await GetJobResults(jobUrl);
            Console.WriteLine("Query results:");
            Console.WriteLine(result);
        }
        else
        {
            Console.WriteLine("Job failed or encountered an error.");
        }
    }
}