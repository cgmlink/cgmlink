using System;

namespace CgmLink.Api.Services;

public interface IMealIngredientRequest
{
    Guid IngredientId { get; }
    Guid ServingId { get; }
    decimal Quantity { get; }
}