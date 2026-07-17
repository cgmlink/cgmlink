using CgmLink.Api.Models;
using CgmLink.Data.Enums;
using System;
using System.Collections.Generic;
using UnitOfMeasurement = CgmLink.Api.Models.UnitOfMeasurement;

namespace CgmLink.Api.Endpoints.Food.List;

public sealed record ListFoodResponse : PagedResponse
{
    public required ICollection<FoodResponse> Food { get; set; } = [];
}

public sealed record FoodResponse
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required FoodType FoodType { get; set; }
    public required DateTimeOffset Created { get; set; }
    public DateTimeOffset? Updated { get; set; }
    public required decimal TotalCalories { get; set; }
    public required decimal TotalCarbs { get; set; }
    public required decimal TotalProtein { get; set; }
    public required decimal TotalFat { get; set; }
    public string? Barcode { get; set; }
    public UnitOfMeasurement? Uom { get; set; }
    public List<FoodIngredientResponse>? Ingredients { get; set; } = [];
}

public sealed record FoodIngredientResponse
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required decimal Quantity { get; set; }
    public required decimal Carbs { get; set; }
    public required decimal Protein { get; set; }
    public required decimal Fat { get; set; }
    public required decimal Calories { get; set; }
    public required UnitOfMeasurement Uom { get; set; }
}
