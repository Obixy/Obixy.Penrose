using Newtonsoft.Json;

namespace DataManager.Domain;

public class GaiaExoplanetJob
{
    [JsonProperty("id")]
    public Guid Id { get; set; }
    public string PartitionKey => Id.ToString() ?? string.Empty;
    public string SourceId { get; init; } = string.Empty;
    public string JobUrl { get; init; } = string.Empty;
    public StatusTypes Status { get; set; }
    public string Name { get; set; } = string.Empty;
    public float Parallax { get; set; }

    public enum StatusTypes
    {
        PENDING,
        ERROR,
        COMPLETED,
        EXECUTING
    }
}
