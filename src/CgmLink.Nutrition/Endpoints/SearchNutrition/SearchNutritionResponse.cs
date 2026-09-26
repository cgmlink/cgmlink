using CgmLink.Nutrition.Endpoints.GetFood;
using System.Collections.Generic;

namespace CgmLink.Nutrition.Endpoints.SearchNutrition;

public sealed record SearchNutritionResponse
{
    public required ICollection<GetFoodResponse> Foods { get; init; } = [];
}
