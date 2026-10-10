using CgmLink.Api.Models;
using System.Collections.Generic;

namespace CgmLink.Api.Endpoints.Meals.ListMeals;

public sealed record ListMealsResponse : PagedResponse
{
    public required ICollection<MealSummaryResponse> Meals { get; init; } = [];
}
