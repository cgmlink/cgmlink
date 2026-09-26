using System.Collections.Generic;

namespace CgmLink.Nutrition.Source;

public sealed class NutritionProduct
{
    public required string ProductId { get; init; }

    public required string Name { get; init; }

    public string? Barcode { get; init; }

    public IReadOnlyCollection<NutritionServing> Servings { get; init; } = [];
}
