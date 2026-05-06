using System.Text.Json.Serialization;

namespace CgmLink.LibreLinkClient.Models;

public record LoginRequest
{
    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("password")]
    public required string Password { get; init; }
}