using Microsoft.Azure.Cosmos;

namespace DataManager.Repositories;

public class ExoplanetStarRepository
{
    private readonly CosmosClient cosmosClient;
    private Database? database;
    private Container? container;

    public ExoplanetStarRepository(CosmosClient cosmosClient, Database? database, Container? container)
    {
        this.cosmosClient = cosmosClient;
        this.database = database;
        this.container = container;
    }

    private async Task<Container> GetContainer(CancellationToken cancellationToken = default)
    {
        database ??= (await cosmosClient.CreateDatabaseIfNotExistsAsync("PenroseDb", cancellationToken: cancellationToken)).Database;
        return container ??= (await database.CreateContainerIfNotExistsAsync("PenroseDbContext", "/PartitionKey", throughput: 400, cancellationToken: cancellationToken)).Container;
    }

    public async Task Create(IDictionary<string, object> exoplanetStar, CancellationToken cancellationToken = default)
    {
        var container = await GetContainer(cancellationToken);
        exoplanetStar.Add("Id", Guid.NewGuid());
        await container.CreateItemAsync(exoplanetStar, new PartitionKey("nasaSpaceApps"), cancellationToken: cancellationToken);
    }
}
