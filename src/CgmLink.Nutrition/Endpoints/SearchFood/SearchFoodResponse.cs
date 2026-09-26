using CgmLink.Nutrition.Endpoints.GetFood;
using System.Collections.Generic;

namespace CgmLink.Nutrition.Endpoints.SearchFood;

public sealed record SearchFoodResponse
{
    public required ICollection<GetFoodResponse> Foods { get; init; } = [];
}
