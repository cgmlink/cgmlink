using CgmLink.Api.Models;
using System;

namespace CgmLink.Api.Endpoints.Ingredients.GetIngredients;

public sealed record GetIngredientResponse
{
    public required Guid Id { get; set; }
    public string? Barcode { get; set; }
    public required string Name { get; set; }
    public required decimal Carbs { get; set; }
    public required decimal Protein { get; set; }
    public required decimal Fat { get; set; }
    public required decimal Calories { get; set; }
    public required UnitOfMeasurement Uom { get; set; }
    public required DateTimeOffset Created { get; set; }
    public DateTimeOffset? Updated { get; set; }
}
