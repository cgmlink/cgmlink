using CgmLink.Api.Endpoints.Readings;
using Microsoft.AspNetCore.Routing;
using System.Diagnostics.CodeAnalysis;
using CgmLink.Api.Endpoints.LibreLink;
using CgmLink.Api.Endpoints.Insulins;
using CgmLink.Api.Endpoints.Insights;
using CgmLink.Api.Endpoints.Sensors;
using CgmLink.Api.Endpoints.Settings;
using CgmLink.Api.Endpoints.Pens;
using CgmLink.Api.Endpoints.Ingredients;

namespace CgmLink.Api.Endpoints;

[ExcludeFromCodeCoverage]
public static class CgmLinkEndpoints
{
    internal static IEndpointRouteBuilder MapCgmLinkEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapSettingsEndpoints();
        endpoints.MapReadingsEndpoints();
        endpoints.MapLibreLinkEndpoints();
        endpoints.MapInsightsEndpoints();
        endpoints.MapInsulinsEndpoints();
        endpoints.MapSensorsEndpoints();
        endpoints.MapPensEndpoints();
        endpoints.MapIngredientsEndpoints();

        return endpoints;
    }
}
