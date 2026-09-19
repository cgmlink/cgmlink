using System.ComponentModel.DataAnnotations;

namespace CgmLink.Nutrition;

/// <summary>
/// Represents the options for configuring the fatsecret nutrition source.
/// </summary>
public sealed class FatSecretOptions
{
    /// <summary>
    /// The fatsecret Consumer Key (OAuth 1.0).
    /// </summary>
    [Required]
    public string ConsumerKey { get; init; } = "";

    /// <summary>
    /// The fatsecret Consumer Secret (OAuth 1.0).
    /// </summary>
    [Required]
    public string ConsumerSecret { get; init; } = "";

    /// <summary>
    /// The authentication scheme used to call the fatsecret API. Supported value is "oauth1".
    /// </summary>
    [Required]
    public string Authentication { get; init; } = "oauth1";

    /// <summary>
    /// The region used to filter fatsecret results. Defaults to "US".
    /// </summary>
    [Required]
    public string Region { get; init; } = "US";

    /// <summary>
    /// The language used for fatsecret results. Ignored unless a region is also specified.
    /// </summary>
    public string Language { get; init; } = "en";

    /// <summary>
    /// The base URL of the fatsecret REST API.
    /// </summary>
    [Required]
    public string ApiBaseUrl { get; init; } = "https://platform.fatsecret.com/rest/";
}