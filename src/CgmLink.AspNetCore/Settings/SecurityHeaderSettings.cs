namespace CgmLink.AspNetCore.Settings;

public sealed record SecurityHeaderSettings
{
    public bool EnableHsts { get; } = true;
    public bool EnableReferrerPolicy { get; } = true;
    public string ReferrerPolicy { get; } = "no-referrer";
    public bool EnableContentSecurityPolicy { get; } = true;
    public string ContentSecurityPolicy { get; }
        = "default-src 'self'; object-src 'none'; frame-ancestors 'none'; sandbox allow-forms allow-same-origin allow-scripts; base-uri 'self';upgrade-insecure-requests;";
}