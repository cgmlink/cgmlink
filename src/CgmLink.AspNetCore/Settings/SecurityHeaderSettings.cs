namespace CgmLink.AspNetCore.Settings;

public sealed record SecurityHeaderSettings
{
    public bool EnableHsts { get; init; } = true;
    public bool EnableReferrerPolicy { get; init; } = true;
    public string ReferrerPolicy { get; init; } = "no-referrer";
    public bool EnableContentSecurityPolicy { get; init; } = true;
    public string ContentSecurityPolicy { get; init; }
        = "default-src 'self'; object-src 'none'; frame-ancestors 'none'; sandbox allow-forms allow-same-origin allow-scripts; base-uri 'self';upgrade-insecure-requests;";
}