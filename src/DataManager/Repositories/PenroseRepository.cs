using DataManager.Domain;
using Microsoft.Azure.Cosmos;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace DataManager.Repositories;

public class PenroseRepository
{
    private readonly CosmosClient cosmosClient;
    private readonly JobsManager jobsManager;
    private Database? database;
    private Container? jobContainer;
    private Container? sourceContainer;

    public PenroseRepository(
        CosmosClient cosmosClient,
        JobsManager jobsManager
    )
    {
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

    public async Task AddSourceBatch(IEnumerable<GaiaSource> gaiaSources, GaiaExoplanetJob exoplanetJob, CancellationToken cancellationToken = default)
    {
        const int batchUnitSize = 100;

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

        await Update(exoplanetJob, cancellationToken);
    }

    public async Task Update(GaiaExoplanetJob exoplanetJob,  CancellationToken cancellationToken = default)
    {
        var container = await GetJobContainer(cancellationToken);

        var updateResponse = await container.UpsertItemAsync(exoplanetJob, new PartitionKey(exoplanetJob.PartitionKey), cancellationToken: cancellationToken);

        if (updateResponse.StatusCode != System.Net.HttpStatusCode.OK)
        {
            throw new Exception($"Update failed with status code {updateResponse.StatusCode}");
        }
    }

    public async Task Store(GaiaExoplanetJob gaiaJob, CancellationToken cancellationToken)
    {
        var jobsContainer = await GetJobContainer(cancellationToken);

        await jobsContainer.CreateItemAsync(
            gaiaJob,
            new PartitionKey(gaiaJob.PartitionKey),
            cancellationToken: cancellationToken
        );

        jobsManager.Add(gaiaJob.SourceId, gaiaJob.JobUrl);
    }

    public record JobGetterFilters(IEnumerable<GaiaExoplanetJob.StatusTypes> Status);
    public async Task<IEnumerable<GaiaExoplanetJob>> GetJobs(JobGetterFilters? jobGetterFilters = null, CancellationToken cancellationToken = default)
    {
        var container = await GetJobContainer(cancellationToken);

        var queryBuilder = new StringBuilder("SELECT * FROM gj");

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

        var query = new QueryDefinition("SELECT TOP 1 * FROM gj WHERE gj.SourceId = @sourceId")
            .WithParameter("@sourceId", sourceId);

        using var resultSetIterator = container.GetItemQueryIterator<GaiaExoplanetJob>(query);

        FeedResponse<GaiaExoplanetJob>? response = null;

        while (resultSetIterator.HasMoreResults)
            response = await resultSetIterator.ReadNextAsync(cancellationToken);

        return response?.FirstOrDefault();
    }

    public async IAsyncEnumerable<IEnumerable<GaiaSource>> GetExplanetStars(Guid jobId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var container = await GetSourceContainer(cancellationToken);

        var countQuery = new QueryDefinition("SELECT VALUE COUNT(gs.JobId) FROM gs WHERE gs.JobId = @jobId")
            .WithParameter("@jobId", jobId);

        using var resultSetCountIterator = container.GetItemQueryIterator<int>(countQuery);
        var count = (await resultSetCountIterator.ReadNextAsync(cancellationToken)).First();

        const int itensPerIteration = 1000;
        var iterationQuantity = (double)count / itensPerIteration;

        for (int i = 0; i < Math.Ceiling(iterationQuantity); i++)
        {
            var queryString = $"SELECT * FROM gs g WHERE g.JobId = @jobId ORDER BY g.JobId OFFSET {i * itensPerIteration} LIMIT {itensPerIteration}";

            var query = new QueryDefinition(queryString)
           .WithParameter("@jobId", jobId);

            using var resultSetIterator = container.GetItemQueryIterator<GaiaSource>(query);

            while (resultSetIterator.HasMoreResults)
                yield return await resultSetIterator.ReadNextAsync(cancellationToken); 
        }
      
    }
}
