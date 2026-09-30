using System;

namespace CgmLink.Api.Services;

public sealed record ResolvedIngredient(
    Guid IngredientId,
    Guid ServingId,
    decimal Quantity,
    decimal Calories,
    decimal Carbs,
    decimal Protein,
    decimal Fat);
