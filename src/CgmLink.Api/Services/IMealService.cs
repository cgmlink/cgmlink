using CgmLink.Data.Entities;

namespace CgmLink.Api.Services;

public interface IMealService
{
    Meal RecalculateNutrition(Meal meal);
}