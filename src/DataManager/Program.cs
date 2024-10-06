using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using DataManager.Repositories;
using DataManager;
using DataManager.Domain;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<JobsManager>();

builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration["PenroseDbConnectionString"];

    return new CosmosClient(connectionString, new CosmosClientOptions
    {
        Serializer = new NewtonsoftJsonCosmosSerializer(NewtonsoftJsonCosmosSerializer.serializerSettings)
    });
});

builder.Services.AddHttpClient<GaiaTapRepository>((sp, options) =>
{
    options.BaseAddress = new("https://gea.esac.esa.int/"); // TODO: Configure enviroment variable
});

builder.Services.AddHostedService<JobBackgroudService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("jobs/{sourceId}/status", async (
    [FromRoute] string sourceId,
    [FromServices] GaiaTapRepository gaiaRepository,
    [FromServices] PenroseRepository penroseRepository,
    CancellationToken cancellationToken = default
) =>
{
    var gaiaJob = await penroseRepository.GetJob(sourceId, cancellationToken);

    if (gaiaJob is null)
    {
        await gaiaRepository.StartGaiaQueryAsync(sourceId, cancellationToken);

        return Results.Ok(GaiaExoplanetJob.StatusTypes.PENDING.ToString());
    }

    var status = await gaiaRepository.CheckJobStatus(gaiaJob.JobUrl, cancellationToken);

    return Results.Ok(status.ToString());
})
.WithName("Jobs")
.WithOpenApi();

using (var scope = app.Services.CreateScope())
{
    var penroseRepository = scope.ServiceProvider.GetRequiredService<PenroseRepository>();
    var jobsManager = scope.ServiceProvider.GetRequiredService<JobsManager>();

    var runningJobs = await penroseRepository.GetJobs(new([GaiaExoplanetJob.StatusTypes.PENDING]));

    foreach (var jobInProgress in runningJobs)
        jobsManager.Add(jobInProgress.SourceId, jobInProgress.JobUrl);
}

app.Run();
