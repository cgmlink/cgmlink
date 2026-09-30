using System;

namespace CgmLink.Api.Services;

public sealed record IngredientReference(
    Guid? IngredientId,
    string? ProductId,
    string ServingId,
    decimal Quantity);
