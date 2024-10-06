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
builder.Services.AddSingleton<PenroseRepository>();

builder.Services.AddHostedService<JobBackgroudService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapPost("jobs", async (
    [FromBody] JobPostRequest request,
    [FromServices] GaiaTapRepository gaiaRepository,
    [FromServices] PenroseRepository penroseRepository,
    CancellationToken cancellationToken = default
) =>
{
    var job = await penroseRepository.GetJob(request.SourceId.Trim(), cancellationToken);

    if (job is not null)
        return Results.Json(
            new { Message = "Job already exist" }, 
            statusCode: StatusCodes.Status403Forbidden
        );

    var jobUrl = await gaiaRepository.StartGaiaQueryAsync(request.SourceId.Trim(), cancellationToken);

    var gaiaJob = new GaiaExoplanetJob
    {
        Id = Guid.NewGuid(),
        Name = request.ExoplanetName,
        JobUrl = jobUrl,
        SourceId = request.SourceId.Trim(),
        Status = GaiaExoplanetJob.StatusTypes.PENDING
    };

    await penroseRepository.Store(gaiaJob, cancellationToken);

    return Results.Created();
});

app.MapGet("jobs/{sourceId}/status", async (
    [FromRoute] string sourceId,
    [FromServices] GaiaTapRepository gaiaRepository,
    [FromServices] PenroseRepository penroseRepository,
    CancellationToken cancellationToken = default
) =>
{
    sourceId = sourceId.Trim();
    var gaiaJob = await penroseRepository.GetJob(sourceId, cancellationToken);

    if (gaiaJob is null)
        return Results.NotFound("Job doesn't exist");

    var status = await gaiaRepository.CheckJobStatus(gaiaJob.JobUrl, cancellationToken);

    return Results.Ok(new { Status = status.ToString() });
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

public record JobPostRequest(string SourceId, string ExoplanetName);