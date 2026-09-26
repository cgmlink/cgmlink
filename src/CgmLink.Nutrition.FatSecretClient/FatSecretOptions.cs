using System.ComponentModel.DataAnnotations;

namespace CgmLink.Nutrition.FatSecretClient;

public sealed class FatSecretOptions
{
    [Required]
    public string ClientId { get; init; } = "";

    [Required]
    public string ClientSecret { get; init; } = "";

    public string Scope { get; init; } = "basic";

    [Required]
    public string ApiBaseUrl { get; init; } = "https://platform.fatsecret.com/rest/";

    [Required]
    public string TokenUrl { get; init; } = "https://oauth.fatsecret.com/connect/token";
}
