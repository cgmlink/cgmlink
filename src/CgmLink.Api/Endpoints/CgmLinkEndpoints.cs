using CgmLink.Api.Endpoints.Meals;
using CgmLink.Api.Endpoints.Readings;
using Microsoft.AspNetCore.Routing;
using System.Diagnostics.CodeAnalysis;
using CgmLink.Api.Endpoints.LibreLink;
using CgmLink.Api.Endpoints.Ingredients;
using CgmLink.Api.Endpoints.Insulins;
using CgmLink.Api.Endpoints.Injections;
using CgmLink.Api.Endpoints.Insights;
using CgmLink.Api.Endpoints.Treatments;
using CgmLink.Api.Endpoints.Sensors;
using CgmLink.Api.Endpoints.Settings;
using CgmLink.Api.Endpoints.Pens;

namespace CgmLink.Api.Endpoints;

[ExcludeFromCodeCoverage]
public static class CgmLinkEndpoints
{
    internal static IEndpointRouteBuilder MapCgmLinkEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapSettingsEndpoints();
        endpoints.MapReadingsEndpoints();
        endpoints.MapMealsEndpoints();
        endpoints.MapLibreLinkEndpoints();
        endpoints.MapIngredientsEndpoints();
        endpoints.MapInsightsEndpoints();
        endpoints.MapInsulinsEndpoints();
        endpoints.MapInjectionsEndpoints();
        endpoints.MapTreatmentsEndpoints();
        endpoints.MapSensorsEndpoints();
        endpoints.MapPensEndpoints();

        return endpoints;
    }
}
