using Newtonsoft.Json;

namespace DataManager.Domain;

public class GaiaSource
{
    [JsonProperty("id")]
    public Guid? Id { get; set; }
    public string PartitionKey => JobId.ToString();
    public Guid JobId { get; set; }
    public Dictionary<string, string> StarData { get; set; } = [];
}
