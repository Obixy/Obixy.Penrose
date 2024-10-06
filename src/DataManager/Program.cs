using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using DataManager.Repositories;
using DataManager;
using DataManager.Domain;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(opt => opt.AddDefaultPolicy(corsPol=> corsPol.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));
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
    var configuration = sp.GetRequiredService<IConfiguration>();

    options.BaseAddress = new(configuration.GetValue<string>("GaiaBaseUrl")!);
});

builder.Services.AddSingleton<PenroseRepository>();

builder.Services.AddHostedService<JobBackgroudService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

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
        Status = GaiaExoplanetJob.StatusTypes.PENDING,
        Parallax = request.ParallaxFromEarth
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
        var json = System.Text.Json.JsonSerializer.Serialize(exoplanetStars.Select(exoplanetStar => exoplanetStar.StarData));
        await httpResponse.WriteAsync($"data: {json}\n\n", cancellationToken: cancellationToken);
        await httpResponse.Body.FlushAsync(cancellationToken);
    }
})
.WithTags("Exoplanets")
.WithOpenApi()
.Produces<IEnumerable<IDictionary<string, string>>>();

app.MapGet("exoplanets/{id}/constellation", async (
    [FromRoute] Guid id,
    [FromServices] PenroseRepository penroseRepository,
    CancellationToken cancellationToken
) =>
{
    var constellations = await penroseRepository.GetExplanetConstellations(id, cancellationToken: cancellationToken);

    var response = constellations.Select(gaiaJob => new ConstellationResponse(
        gaiaJob.Id,
        gaiaJob.Name,
        gaiaJob.Votes,
        gaiaJob.Points.Select(p => new ConstellationResponse.PointResponse(p.SourceId, p.X, p.Y, p.Z))
    ));

    return Results.Ok(response);
})
.WithTags("Constellations")
.WithOpenApi()
.Produces<IEnumerable<ConstellationResponse>>();

app.MapPost("exoplanets/{id}/constellation", async (
    [FromBody] ConstellationRequest request,
    [FromRoute] Guid id,
    [FromServices] PenroseRepository penroseRepository,
    CancellationToken cancellationToken = default
) =>
{
    await penroseRepository.Store(new Constellation { 
        Id = Guid.NewGuid(),
        JobId = id,
        Name = request.Name,
        Points = request.Points.Select(x => new Constellation.Point { SourceId =x.SourceId, X = x.X, Y=x.Y, Z = x.Z }),
        Votes = 0
    }, cancellationToken);

    return Results.Created();
})
.WithTags("Constellations")
.Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
.Produces(StatusCodes.Status201Created);

using (var scope = app.Services.CreateScope())
{
    var penroseRepository = scope.ServiceProvider.GetRequiredService<PenroseRepository>();
    var jobsManager = scope.ServiceProvider.GetRequiredService<JobsManager>();

    var runningJobs = await penroseRepository.GetJobs(new([GaiaExoplanetJob.StatusTypes.PENDING]));

    foreach (var jobInProgress in runningJobs)
        jobsManager.Add(jobInProgress.SourceId, jobInProgress.JobUrl);
}

app.Run();

public record JobPostRequest(string SourceId, string ExoplanetName, float ParallaxFromEarth);
public record JobStatusResponse(string Status);
public record ExoplanetGetResponse(Guid Id, string Name, float Parallax);
public record ErrorResponse(string Message);

public record ConstellationRequest(string Name, IEnumerable<ConstellationRequest.PointRequest> Points)
{
    public record PointRequest(
        string SourceId,
        float X,
        float Y,
        float Z
    );
}

public record ConstellationResponse(Guid Id, string Name, int Votes, IEnumerable<ConstellationResponse.PointResponse> Points)
{
    public record PointResponse(
        string SourceId,
        float X,
        float Y,
        float Z
    );
}