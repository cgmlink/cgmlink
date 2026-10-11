using CgmLink.Data.Entities;
using CgmLink.Api.Endpoints.Treatments.GetTreatment;
using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data.Repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Services;

public sealed class TreatmentService : ITreatmentService
{
    private readonly IRepository<Treatment> _treatmentsRepository;
    private readonly IIngredientsService _ingredientsService;
    private readonly IMealService _mealService;

    public TreatmentService(
        IRepository<Treatment> treatmentsRepository,
        IIngredientsService ingredientsService,
        IMealService mealService)
    {
        _treatmentsRepository = treatmentsRepository;
        _ingredientsService = ingredientsService;
        _mealService = mealService;
    }

    public async Task<GetTreatmentResponse> GetTreatmentAsync(
        Guid treatmentId, Guid userId, CancellationToken cancellationToken = default)
    {
        var treatment = await _treatmentsRepository.GetAll(new FindOptions { IsAsNoTracking = true })
            .Include(t => t.Meals)
                .ThenInclude(tm => tm.Meal)
            .Include(t => t.Ingredients)
                .ThenInclude(ti => ti.Ingredient)
            .Include(t => t.Ingredients)
                .ThenInclude(ti => ti.Serving)
            .Include(t => t.NutritionIngredients)
                .ThenInclude(ti => ti.NutritionIngredient)
            .Include(t => t.NutritionIngredients)
                .ThenInclude(ti => ti.Serving)
            .Include(t => t.Injection!.Insulin)
            .Include(t => t.Reading)
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Id == treatmentId && t.UserId == userId && t.Deleted == null, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("TREATMENT_NOT_FOUND");

        var servings = await _ingredientsService.GetNutritionServingsAsync(
            treatment.NutritionIngredients.Select(i => (i.NutritionIngredient, i.Serving)),
            treatment.UserId, cancellationToken).ConfigureAwait(false);
        var nutritionIngredients = treatment.NutritionIngredients.Select(i =>
        {
            var resolved = servings[i.ServingId];
            return GetTreatmentNutritionIngredientResponse.ToResponse(i, resolved.Product, resolved.Serving);
        }).ToList();

        return GetTreatmentResponse.ToResponse(treatment) with { NutritionIngredients = nutritionIngredients };
    }

    public Treatment CreateTreatment(
        IEnumerable<ITreatmentMealRequest> meals,
        IEnumerable<IMealIngredientRequest> ingredients,
        Dictionary<Guid, Meal> mealLookup,
        Dictionary<Guid, Ingredient> ingredientLookup,
        Guid userId,
        Guid? readingId,
        Guid? injectionId,
        DateTimeOffset created,
        IEnumerable<ResolvedIngredient> nutritionIngredients)
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

        var resolvedIngredients = nutritionIngredients.ToList();
        foreach (var ingredient in resolvedIngredients)
        {
            treatment.NutritionIngredients.Add(new TreatmentNutritionIngredient
            {
                TreatmentId = treatmentId,
                NutritionIngredientId = ingredient.IngredientId,
                ServingId = ingredient.ServingId,
                Quantity = ingredient.Quantity,
                Created = created,
            });
        }

        CalculateTreatmentNutrition(treatment, resolvedIngredients);

        return treatment;
    }

    public async Task<Treatment> UpdateTreatmentFoodsAsync(
        Treatment treatment,
        IEnumerable<ITreatmentMealRequest>? meals,
        IEnumerable<IMealIngredientRequest>? ingredients,
        IEnumerable<IngredientReference>? nutritionIngredients,
        DateTimeOffset updated,
        CancellationToken cancellationToken = default)
    {
        if (meals is null && ingredients is null && nutritionIngredients is null)
        {
            return treatment;
        }

        var requestedMeals = meals?.ToList();
        var requestedIngredients = ingredients?.ToList();
        var mealLookup = new Dictionary<Guid, Meal>();
        if (requestedMeals is not null)
        {
            var existingIds = treatment.Meals.Select(m => m.MealId).ToHashSet();
            mealLookup = await _mealService.GetValidatedMealsAsync(
                requestedMeals.Where(m => !existingIds.Contains(m.MealId)), treatment.UserId, cancellationToken)
                .ConfigureAwait(false);
        }

        var ingredientLookup = new Dictionary<Guid, Ingredient>();
        if (requestedIngredients is not null)
        {
            var existingIngredients = treatment.Ingredients.ToDictionary(i => i.IngredientId);
            var toValidate = requestedIngredients.Where(i =>
                !existingIngredients.TryGetValue(i.IngredientId, out var existing) ||
                (existing.Ingredient?.Deleted is null && existing.ServingId != i.ServingId));
            ingredientLookup = await _ingredientsService.GetValidatedIngredientsAsync(
                toValidate.Select(IngredientReference.FromPersonal), treatment.UserId, cancellationToken)
                .ConfigureAwait(false);
        }

        var resolvedIngredients = nutritionIngredients is null ? null :
            await _ingredientsService.ResolveIngredientsAsync(nutritionIngredients, treatment.UserId, cancellationToken)
                .ConfigureAwait(false);

        if (requestedMeals is not null)
        {
            UpdateTreatmentMeals(treatment, requestedMeals, mealLookup, updated);
        }

        if (requestedIngredients is not null)
        {
            UpdateTreatmentIngredients(treatment, requestedIngredients, ingredientLookup, updated);
        }

        if (resolvedIngredients is not null)
        {
            UpdateTreatmentNutritionIngredients(treatment, resolvedIngredients.ToList(), updated);
        }

        return await RecalculateTreatmentNutritionAsync(treatment, cancellationToken).ConfigureAwait(false);
    }

    private static void UpdateTreatmentNutritionIngredients(
        Treatment treatment,
        IReadOnlyCollection<ResolvedIngredient> ingredients,
        DateTimeOffset updated)
    {
        var existingByIngredientId = treatment.NutritionIngredients.ToDictionary(i => i.NutritionIngredientId);
        var requestedIds = ingredients.Select(i => i.IngredientId).ToHashSet();
        foreach (var ingredient in ingredients)
        {
            if (existingByIngredientId.TryGetValue(ingredient.IngredientId, out var existing))
            {
                existing.ServingId = ingredient.ServingId;
                existing.Quantity = ingredient.Quantity;
            }
            else
            {
                treatment.NutritionIngredients.Add(new TreatmentNutritionIngredient
                {
                    TreatmentId = treatment.Id,
                    NutritionIngredientId = ingredient.IngredientId,
                    ServingId = ingredient.ServingId,
                    Quantity = ingredient.Quantity,
                    Created = updated,
                });
            }
        }

        foreach (var removed in treatment.NutritionIngredients.Where(i => !requestedIds.Contains(i.NutritionIngredientId)).ToList())
        {
            treatment.NutritionIngredients.Remove(removed);
        }
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

    public async Task<Treatment> RecalculateTreatmentNutritionAsync(
        Treatment treatment,
        CancellationToken cancellationToken = default)
    {
        var nutritionIngredients = treatment.NutritionIngredients.Count == 0 ? [] :
            await _ingredientsService.ResolveIngredientsAsync(
                treatment.NutritionIngredients.Select(i => (i.NutritionIngredientId, i.ServingId, i.Quantity)),
                treatment.UserId, cancellationToken).ConfigureAwait(false);
        return CalculateTreatmentNutrition(treatment, nutritionIngredients);
    }

    private static Treatment CalculateTreatmentNutrition(
        Treatment treatment,
        IEnumerable<ResolvedIngredient> nutritionIngredients)
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

        foreach (var ingredient in nutritionIngredients)
        {
            calories += ingredient.Calories * ingredient.Quantity;
            carbs += ingredient.Carbs * ingredient.Quantity;
            protein += ingredient.Protein * ingredient.Quantity;
            fat += ingredient.Fat * ingredient.Quantity;
        }

        treatment.Calories = calories;
        treatment.Carbs = carbs;
        treatment.Protein = protein;
        treatment.Fat = fat;

        return treatment;
    }
}
