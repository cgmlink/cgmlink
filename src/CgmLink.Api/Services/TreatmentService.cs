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

    public Treatment UpdateTreatmentFoods(
        Treatment treatment,
        IEnumerable<ITreatmentMealRequest>? meals,
        IEnumerable<IMealIngredientRequest>? ingredients,
        Dictionary<Guid, Meal> mealLookup,
        Dictionary<Guid, Ingredient> ingredientLookup,
        DateTimeOffset updated)
    {
        if (meals is not null)
        {
            UpdateTreatmentMeals(treatment, meals, mealLookup, updated);
        }

        if (ingredients is not null)
        {
            UpdateTreatmentIngredients(treatment, ingredients, ingredientLookup, updated);
        }

        RecalculateTreatmentNutrition(treatment);

        return treatment;
    }

    private static void UpdateTreatmentMeals(
        Treatment treatment,
        IEnumerable<ITreatmentMealRequest> meals,
        Dictionary<Guid, Meal> mealLookup,
        DateTimeOffset updated)
    {
        var requestedMeals = meals.ToList();
        var requestedMealIds = requestedMeals.Select(m => m.MealId).ToHashSet();
        var existingByMealId = treatment.Meals.ToDictionary(m => m.MealId);

        foreach (var requested in requestedMeals)
        {
            if (existingByMealId.TryGetValue(requested.MealId, out var existing))
            {
                existing.Quantity = requested.Quantity;
            }
            else
            {
                treatment.Meals.Add(new TreatmentMeal
                {
                    Id = Guid.NewGuid(),
                    TreatmentId = treatment.Id,
                    MealId = requested.MealId,
                    Meal = mealLookup[requested.MealId],
                    Quantity = requested.Quantity,
                    Created = updated,
                });
            }
        }

        foreach (var removed in treatment.Meals.Where(m => !requestedMealIds.Contains(m.MealId)).ToList())
        {
            treatment.Meals.Remove(removed);
        }
    }

    private static void UpdateTreatmentIngredients(
        Treatment treatment,
        IEnumerable<IMealIngredientRequest> ingredients,
        Dictionary<Guid, Ingredient> ingredientLookup,
        DateTimeOffset updated)
    {
        var requestedIngredients = ingredients.ToList();
        var requestedIngredientIds = requestedIngredients.Select(i => i.IngredientId).ToHashSet();
        var existingByIngredientId = treatment.Ingredients.ToDictionary(i => i.IngredientId);

        foreach (var requested in requestedIngredients)
        {
            if (existingByIngredientId.TryGetValue(requested.IngredientId, out var existing))
            {
                existing.Quantity = requested.Quantity;

                if (existing.ServingId != requested.ServingId &&
                    ingredientLookup.TryGetValue(requested.IngredientId, out var ingredient))
                {
                    var serving = ingredient.Servings.Single(s => s.Id == requested.ServingId);
                    existing.ServingId = serving.Id;
                    existing.Serving = serving;
                    existing.Ingredient = ingredient;
                }
            }
            else
            {
                var ingredient = ingredientLookup[requested.IngredientId];
                treatment.Ingredients.Add(new TreatmentIngredient
                {
                    Id = Guid.NewGuid(),
                    TreatmentId = treatment.Id,
                    IngredientId = requested.IngredientId,
                    Ingredient = ingredient,
                    ServingId = requested.ServingId,
                    Serving = ingredient.Servings.Single(s => s.Id == requested.ServingId),
                    Quantity = requested.Quantity,
                    Created = updated,
                });
            }
        }

        foreach (var removed in treatment.Ingredients.Where(i => !requestedIngredientIds.Contains(i.IngredientId)).ToList())
        {
            treatment.Ingredients.Remove(removed);
        }
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