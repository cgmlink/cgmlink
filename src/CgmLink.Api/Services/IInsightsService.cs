using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Api.Endpoints.Insights.TotalNutrition;
using CgmLink.Api.Endpoints.Insights.AverageNutrition;
using CgmLink.Api.Endpoints.Insights.MealGlucose;

namespace CgmLink.Api.Services;

public interface IInsightsService
{
    Task<List<MealGlucoseResponse>> GetMealGlucoseAsync(
        Guid userId, DateTimeOffset from, DateTimeOffset to, int intervalMinutes = MealGlucoseConstants.DefaultIntervalMinutes,
        CancellationToken cancellationToken = default);

    Task<TotalNutritionResponse> GetTotalNutritionAsync(
        Guid userId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

    Task<AverageNutritionResponse> GetAverageNutritionAsync(
        Guid userId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
}
