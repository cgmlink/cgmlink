using System.ComponentModel.DataAnnotations;

namespace CgmLink.Nutrition.FatSecretClient;

public sealed class FatSecretOptions
{
    [Required]
    public string ConsumerKey { get; init; } = "";

    [Required]
    public string ConsumerSecret { get; init; } = "";

    [Required]
    public string Authentication { get; init; } = "oauth1";

    [Required]
    public string Region { get; init; } = "US";

    public string Language { get; init; } = "en";

    [Required]
    public string ApiBaseUrl { get; init; } = "https://platform.fatsecret.com/rest/";
}