using Newtonsoft.Json;

namespace DataManager.Domain;

public class GaiaExoplanetJob
{
    [JsonProperty("id")]
    public Guid? Id { get; set; }
    public string PartitionKey => Id?.ToString() ?? "";
    public string SourceId { get; init; } = "";
    public string JobUrl { get; init; } = "";
    public StatusTypes Status { get; set; }

    public enum StatusTypes
    {
        PENDING,
        ERROR,
        COMPLETED,
        EXECUTING
    }
}
