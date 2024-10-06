using Newtonsoft.Json;

namespace DataManager.Domain;

public class Constellation
{
    [JsonProperty("id")]
    public Guid Id { get; set; }
    public string PartitionKey => JobId.ToString();
    public Guid JobId { get; set; }
    public string Name { get; set; } = "";
    public int Votes { get; set; }
    public IEnumerable<Point> Points { get; set; } = [];

    public class Point
    {
        public string SourceId { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }
}
