using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using DataManager.Repositories;
using DataManager;
using DataManager.Domain;
using System.Text.Json;

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
            new ErrorResponse("Job already exist"), 
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
})
.WithTags("Jobs")
.Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
.Produces(StatusCodes.Status201Created);

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
        return Results.NotFound(new ErrorResponse("Job doesn't exist"));

    var status = await gaiaRepository.CheckJobStatus(gaiaJob.JobUrl, cancellationToken);

    return Results.Ok(new JobStatusResponse(status.ToString()));
})
.WithTags("Jobs")
.Produces<ErrorResponse>(StatusCodes.Status404NotFound)
.Produces<JobStatusResponse>();

app.MapGet("exoplanets", async (
    [FromServices] PenroseRepository penroseRepository,
    CancellationToken cancellationToken
) =>
{
    var gaiaJobs = await penroseRepository.GetJobs(cancellationToken: cancellationToken);

    var response = gaiaJobs.Select(gaiaJob => new ExoplanetGetResponse(
        gaiaJob.Id,
        gaiaJob.Name,
        gaiaJob.Parallax
    ));

    return Results.Ok(response);
})
.WithTags("Exoplanets")
.WithOpenApi()
.Produces<ExoplanetGetResponse>();

app.MapGet("exoplanets/{id}/stars", async (
    [FromRoute] Guid id,
    [FromServices] PenroseRepository penroseRepository,
    HttpResponse httpResponse,
    CancellationToken cancellationToken
) =>
{
    httpResponse.ContentType = "text/event-stream";
    
    await foreach (var exoplanetStars in penroseRepository.GetExplanetStars(id, cancellationToken))
    {
        var json = JsonSerializer.Serialize(exoplanetStars.Select(exoplanetStar => exoplanetStar.StarData));
        await httpResponse.WriteAsync($"data: {json}\n\n", cancellationToken: cancellationToken);
        await httpResponse.Body.FlushAsync(cancellationToken);
    }
})
.WithTags("Exoplanets")
.WithOpenApi()
.Produces<IEnumerable<IDictionary<string, string>>>();

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
public record JobStatusResponse(string Status);
public record ExoplanetGetResponse(Guid Id, string Name, float Parallax);
public record ErrorResponse(string Message);