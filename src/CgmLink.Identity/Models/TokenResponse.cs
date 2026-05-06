using System.Text.Json.Serialization;

namespace CgmLink.Identity.Models;

public sealed record TokenResponse
{
    public required string Token { get; init; }

    [JsonIgnore]
    public string? RefreshToken { get; init; }
}