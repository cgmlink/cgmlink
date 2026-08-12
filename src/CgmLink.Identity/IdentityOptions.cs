using System.ComponentModel.DataAnnotations;

namespace CgmLink.Identity;

public sealed class IdentityOptions
{
    [Required]
    [MinLength(32)]
    public string TokenSigningKey { get; init; } = "";

    [Required] public int TokenExpirationInMinutes { get; init; } = 60;
    
    [Required] public string Issuer { get; init; } = "";

    [Required] public string Audience { get; init; } = "";

    [Required] public string RefreshTokenCookieName { get; set; } = "";

    [Required] public string RefreshTokenCookiePath { get; set; } = "/api";

    [Required] public int RefreshTokenExpirationInDays { get; set; } = 30;

    public bool RequireEmailVerification { get; set; } = true;

    public string? VerifyEmailBaseUri { get; set; }
}