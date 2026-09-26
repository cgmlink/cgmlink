using CgmLink.Nutrition.Api.Endpoints.GetFood;
using System.Collections.Generic;

namespace CgmLink.Nutrition.Api.Endpoints.SearchNutrition;

public sealed record SearchNutritionResponse
{
    public required ICollection<GetFoodResponse> Foods { get; init; } = [];
}
