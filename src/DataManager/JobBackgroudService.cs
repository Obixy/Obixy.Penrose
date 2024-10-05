
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
            var gaiaRepository = serviceProvider.GetRequiredService<GaiaRepository>();

            foreach (var job in jobsManager.GetJobs())
            {
                var status = await gaiaRepository.CheckJobStatus(job.Value, stoppingToken);

                if (status is GaiaJob.StatusTypes.RUNNING)
                    continue;

                if (status is GaiaJob.StatusTypes.ERROR)
                {
                    jobsManager.Remove(job.Key);
                    continue;
                }

                if (status is GaiaJob.StatusTypes.COMPLETED)
                {
                    var result = await gaiaRepository.GetJobResults(job.Value, stoppingToken);

                    await gaiaRepository.AddJobResult(result, stoppingToken);
                    jobsManager.Remove(job.Key);
                }
            }

            await Task.Delay(cicleTimeInSeconds, stoppingToken);
        }
    }
}
