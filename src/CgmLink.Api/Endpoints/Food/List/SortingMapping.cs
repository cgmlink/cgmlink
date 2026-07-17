using System.Collections.Generic;
using CgmLink.Api.Models;

namespace CgmLink.Api.Endpoints.Food.List;

internal static class SortingMapping
{
    internal static readonly Dictionary<FoodSortOrder, string> SortPropertyMap = new()
    {
        [FoodSortOrder.Created] = nameof(FoodResponse.Created),
        [FoodSortOrder.Updated] = nameof(FoodResponse.Updated),
        [FoodSortOrder.Name] = nameof(FoodResponse.Name),
        [FoodSortOrder.TotalCalories] = nameof(FoodResponse.TotalCalories),
        [FoodSortOrder.TotalCarbs] = nameof(FoodResponse.TotalCarbs),
        [FoodSortOrder.TotalProtein] = nameof(FoodResponse.TotalProtein),
        [FoodSortOrder.TotalFat] = nameof(FoodResponse.TotalFat)
    };
}
