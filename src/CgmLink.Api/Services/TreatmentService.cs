using CgmLink.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CgmLink.Api.Services;

public sealed class TreatmentService : ITreatmentService
{
    public Treatment CreateTreatment(
        IEnumerable<ITreatmentMealRequest> meals,
        IEnumerable<IMealIngredientRequest> ingredients,
        Dictionary<Guid, Meal> mealLookup,
        Dictionary<Guid, Ingredient> ingredientLookup,
        Guid userId,
        Guid? readingId,
        Guid? injectionId,
        DateTimeOffset created)
    {
        var treatmentId = Guid.NewGuid();

        var treatment = new Treatment
        {
            Id = treatmentId,
            UserId = userId,
            ReadingId = readingId,
            InjectionId = injectionId,
            Created = created,
            Calories = 0m,
            Carbs = 0m,
            Protein = 0m,
            Fat = 0m,
            Meals = meals.Select(m => new TreatmentMeal
            {
                Id = Guid.NewGuid(),
                TreatmentId = treatmentId,
                MealId = m.MealId,
                Meal = mealLookup[m.MealId],
                Quantity = m.Quantity,
                Created = created,
            }).ToList(),
            Ingredients = ingredients.Select(i => new TreatmentIngredient
            {
                Id = Guid.NewGuid(),
                TreatmentId = treatmentId,
                IngredientId = i.IngredientId,
                Ingredient = ingredientLookup[i.IngredientId],
                ServingId = i.ServingId,
                Serving = ingredientLookup[i.IngredientId].Servings.Single(s => s.Id == i.ServingId),
                Quantity = i.Quantity,
                Created = created,
            }).ToList(),
        };

        RecalculateTreatmentNutrition(treatment);

        return treatment;
    }

    public Treatment RecalculateTreatmentNutrition(Treatment treatment)
    {
        var calories = 0m;
        var carbs = 0m;
        var protein = 0m;
        var fat = 0m;

        foreach (var treatmentMeal in treatment.Meals)
        {
            var meal = treatmentMeal.Meal;
            if (meal is null)
            {
                continue;
            }

            calories += meal.Calories * treatmentMeal.Quantity;
            carbs += meal.Carbs * treatmentMeal.Quantity;
            protein += meal.Protein * treatmentMeal.Quantity;
            fat += meal.Fat * treatmentMeal.Quantity;
        }

        foreach (var treatmentIngredient in treatment.Ingredients)
        {
            var serving = treatmentIngredient.Serving;
            if (serving is null)
            {
                continue;
            }

            calories += serving.Calories * treatmentIngredient.Quantity;
            carbs += serving.Carbs * treatmentIngredient.Quantity;
            protein += serving.Protein * treatmentIngredient.Quantity;
            fat += serving.Fat * treatmentIngredient.Quantity;
        }

        treatment.Calories = calories;
        treatment.Carbs = carbs;
        treatment.Protein = protein;
        treatment.Fat = fat;

        return treatment;
    }
}