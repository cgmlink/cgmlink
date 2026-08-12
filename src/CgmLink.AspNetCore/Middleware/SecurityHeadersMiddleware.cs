using System;
using System.Threading.Tasks;
using CgmLink.AspNetCore.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CgmLink.AspNetCore.Middleware;

internal sealed class SecurityHeadersMiddleware : IMiddleware
{
    private readonly SecurityHeaderSettings _options;

    public SecurityHeadersMiddleware(IOptions<SecurityHeaderSettings> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var headers = context.Response.Headers;

        // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Content-Type-Options
        EnsureContains(headers, "X-Content-Type-Options", "nosniff");
        // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Frame-Options
        EnsureContains(headers, "X-Frame-Options", "SAMEORIGIN");
        if (_options.EnableReferrerPolicy)
        {
            // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Referrer-Policy
            EnsureContains(headers, "Referrer-Policy", _options.ReferrerPolicy);
        }
        if (_options.EnableContentSecurityPolicy && context.Request.Path.StartsWithSegments("/swagger"))
        {
            // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Security-Policy
            EnsureContains(headers, "Content-Security-Policy", _options.ContentSecurityPolicy);
        }

        await next(context).ConfigureAwait(false);
    }

    private static bool EnsureContains(IHeaderDictionary headers, string key, string value)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentNullException(nameof(key));
        }
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (!headers.ContainsKey(key))
        {
            headers.Add(key, value);
            return true;
        }

        return false;
    }
}