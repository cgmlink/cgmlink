namespace CgmLink.AspNetCore.Settings;

internal sealed record SecurityHeaderSettings(
    bool EnableHsts = true,
    bool EnableReferrerPolicy = true,
    string ReferrerPolicy = "no-referrer",
    bool EnableContentSecurityPolicy = true,
    string ContentSecurityPolicy =
        "default-src 'self'; object-src 'none'; frame-ancestors 'none'; sandbox allow-forms allow-same-origin allow-scripts; base-uri 'self';upgrade-insecure-requests;");