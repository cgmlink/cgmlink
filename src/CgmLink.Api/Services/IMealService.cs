using CgmLink.Data.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Services;

public interface IMealService
{
    Meal RecalculateNutrition(Meal meal);
    Task RecalculateNutritionForIngredient(Guid ingredientId, CancellationToken cancellationToken = default);
}