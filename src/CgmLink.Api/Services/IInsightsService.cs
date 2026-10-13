using System;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Api.Endpoints.Insights.TotalNutrition;
using CgmLink.Api.Endpoints.Insights.AverageNutrition;

namespace CgmLink.Api.Services;

public interface IInsightsService
{
    Task<TotalNutritionResponse> GetTotalNutritionAsync(
        Guid userId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

    Task<AverageNutritionResponse> GetAverageNutritionAsync(
        Guid userId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
}
