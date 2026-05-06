using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace CgmLink.Data;

[ExcludeFromCodeCoverage]
public static class HealthChecksBuilderExtensions
{
    public static IHealthChecksBuilder AddDatabaseHealthChecks(this IHealthChecksBuilder builder)
    {
        return builder.AddDbContextCheck<CgmLinkDbContext>("CgmLink-Database");
    }
}