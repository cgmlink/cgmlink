using System.Text.Json.Serialization;

namespace CgmLink.LibreLinkClient.Models;

public sealed record SensorData
{
    [JsonPropertyName("sn")]
    public string SensorId { get; init; } = "";
    [JsonPropertyName("a")]
    public int Started { get; init; }
}
