using CgmLink.AspNetCore.Middleware;
using CgmLink.AspNetCore.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CgmLink.AspNetCore.Extensions;

public static class SecurityExtensions
{
    public static IServiceCollection AddSecurity(this IServiceCollection services, IConfigurationSection config)
    {
        services.AddOptions<SecurityHeaderSettings>().Bind(config)
            .Validate(settings =>
            {
                if (settings.EnableReferrerPolicy && string.IsNullOrEmpty(settings.ReferrerPolicy))
                {
                    return false;
                }

                if (settings.EnableContentSecurityPolicy && string.IsNullOrEmpty(settings.ContentSecurityPolicy))
                {
                    return false;
                }
                return true;
            }, "Invalid security header settings")
            .ValidateOnStart();
        return services.AddSingleton<SecurityHeadersMiddleware>();
    }


    public static IApplicationBuilder UseSecurity(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}