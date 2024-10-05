using System.Collections.Concurrent;
namespace DataManager;

public interface IJobManagerPublisher
{
    void Add(string sourceId, string jobUrl);
}

public interface IJobManagetConsumer
{
    IDictionary<string, string> GetJobs();
    void Remove(string sourceId);
}

public class JobsManager : IJobManagerPublisher, IJobManagetConsumer
{
    public IDictionary<string, string> SourceIdJobUrlDict { get; set; } = new ConcurrentDictionary<string, string>();

    public void Add(string sourceId, string jobUrl)
    {
        _ = SourceIdJobUrlDict.TryAdd(sourceId, jobUrl);
    }

    public IDictionary<string, string> GetJobs()
    {
        return new Dictionary<string, string>(SourceIdJobUrlDict);
    }

    public void Remove(string sourceId)
    {
        SourceIdJobUrlDict.Remove(sourceId);
    }
}
