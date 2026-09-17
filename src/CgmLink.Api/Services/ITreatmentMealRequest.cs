using System;

namespace CgmLink.Api.Services;

public interface ITreatmentMealRequest
{
    Guid MealId { get; }
    decimal Quantity { get; }
}