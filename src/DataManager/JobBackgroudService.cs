
using DataManager.Domain;
using DataManager.Repositories;

namespace DataManager;

public class JobBackgroudService : BackgroundService
{
    private const int cicleTimeInSeconds = 5 * 1000;

    private readonly IServiceProvider serviceProvider;
    private readonly JobsManager jobsManager;

    public JobBackgroudService(IServiceProvider serviceProvider, JobsManager jobsManager)
    {
        this.serviceProvider = serviceProvider;
        this.jobsManager = jobsManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            var gaiaRepository = serviceProvider.GetRequiredService<GaiaTapRepository>();

            foreach (var (sourceId, jobUrl) in jobsManager.GetJobs())
            {
                var status = await gaiaRepository.CheckJobStatus(jobUrl, stoppingToken);

                if (status is GaiaExoplanetJob.StatusTypes.ERROR)
                {
                    jobsManager.Remove(sourceId);
                    continue;
                }

                if (status is GaiaExoplanetJob.StatusTypes.COMPLETED)
                {
                    var storedJob = await gaiaRepository.GetJob(sourceId,stoppingToken);

                    if (storedJob is null)
                        throw new Exception($"StoredJob was null for: sourceId: {sourceId}, jobUrl:{jobUrl}");

                    var sourceBatch = await gaiaRepository.GetJobResults(storedJob, jobUrl, stoppingToken);

                    await gaiaRepository.AddSourceBatch(sourceBatch, storedJob, stoppingToken);
                    jobsManager.Remove(sourceId);
                }
            }

            await Task.Delay(cicleTimeInSeconds, stoppingToken);
        }
    }
}
