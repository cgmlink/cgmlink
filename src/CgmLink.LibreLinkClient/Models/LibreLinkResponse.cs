using System.Text.Json.Serialization;

namespace CgmLink.LibreLinkClient.Models;

public record LibreLinkResponse<TModel>
{
    [JsonPropertyName("status")]
    public long Status { get; set; }

    [JsonPropertyName("data")]
    public TModel? Data { get; set; }
}