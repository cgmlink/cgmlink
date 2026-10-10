using CgmLink.AspNetCore.Exceptions;
using CgmLink.Api.Endpoints.Ingredients;
using CgmLink.Api.Endpoints.Meals.GetMeal;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Resources;
using CgmLink.Nutrition;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Services;

public sealed class MealService : IMealService
{
    private readonly IRepository<Meal> _mealsRepository;
    private readonly INutritionCatalog _nutritionCatalog;
    private readonly IIngredientsService _ingredientsService;
    private readonly IRepository<NutritionIngredient> _nutritionIngredientsRepository;

    public MealService(
        IRepository<Meal> mealsRepository,
        INutritionCatalog nutritionCatalog,
        IIngredientsService ingredientsService,
        IRepository<NutritionIngredient> nutritionIngredientsRepository)
    {
        _mealsRepository = mealsRepository;
        _nutritionCatalog = nutritionCatalog;
        _ingredientsService = ingredientsService;
        _nutritionIngredientsRepository = nutritionIngredientsRepository;
    }

    public async Task<GetMealResponse> GetMealAsync(Guid mealId, Guid userId, CancellationToken cancellationToken = default)
    {
        var meal = await _mealsRepository.GetAll(new FindOptions { IsAsNoTracking = true })
            .AsSplitQuery()
            .Include(m => m.Ingredients)
                .ThenInclude(mi => mi.Ingredient)
            .Include(m => m.Ingredients)
                .ThenInclude(mi => mi.Serving)
            .Include(m => m.NutritionIngredients)
                .ThenInclude(mi => mi.NutritionIngredient)
            .Include(m => m.NutritionIngredients)
                .ThenInclude(mi => mi.Serving)
            .FirstOrDefaultAsync(m => m.Id == mealId && m.UserId == userId && m.Deleted == null, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("MEAL_NOT_FOUND");

        var products = new Dictionary<string, IngredientResponse>();
        var nutritionIngredients = new List<MealNutritionIngredientResponse>();
        foreach (var mealIngredient in meal.NutritionIngredients)
        {
            var identity = mealIngredient.NutritionIngredient;
            if (identity is null || identity.Source != _nutritionCatalog.Source)
            {
                throw new BadRequestException(ValidationMessages.IngredientIdInvalid);
            }
            if (mealIngredient.Serving is null)
            {
                throw new BadRequestException(ValidationMessages.IngredientServingIdInvalid);
            }
            if (!products.TryGetValue(identity.ProductId, out var product))
            {
                product = await _ingredientsService.GetIngredientAsync(
                    identity.ProductId, meal.UserId, cancellationToken, IngredientType.External).ConfigureAwait(false);
                products.Add(identity.ProductId, product);
            }
            var serving = product.Servings.SingleOrDefault(s => s.ServingId == mealIngredient.Serving.ServingId)
                ?? throw new BadRequestException(ValidationMessages.IngredientServingIdInvalid);
            nutritionIngredients.Add(MealNutritionIngredientResponse.ToResponse(mealIngredient, product, serving));
        }

        return GetMealResponse.ToResponse(meal, meal.Ingredients.Count + meal.NutritionIngredients.Count)
            with { NutritionIngredients = nutritionIngredients };
    }

    public async Task<Meal> RecalculateMealsNutritionAsync(Meal meal, CancellationToken cancellationToken = default)
    {
        var calories = 0m;
        var carbs = 0m;
        var protein = 0m;
        var fat = 0m;

        foreach (var mealIngredient in meal.Ingredients)
        {
            var serving = mealIngredient.Serving;
            if (serving is null)
            {
                continue;
            }

            calories += serving.Calories * mealIngredient.Quantity;
            carbs += serving.Carbs * mealIngredient.Quantity;
            protein += serving.Protein * mealIngredient.Quantity;
            fat += serving.Fat * mealIngredient.Quantity;
        }

        var products = new Dictionary<string, Nutrition.Source.NutritionProduct>();
        foreach (var mealIngredient in meal.NutritionIngredients)
        {
            var ingredient = mealIngredient.NutritionIngredient;
            if (ingredient is null || ingredient.Source != _nutritionCatalog.Source)
            {
                throw new BadRequestException(ValidationMessages.IngredientIdInvalid);
            }

            if (mealIngredient.Serving is null)
            {
                throw new BadRequestException(ValidationMessages.IngredientServingIdInvalid);
            }

            if (!products.TryGetValue(ingredient.ProductId, out var product))
            {
                product = await _nutritionCatalog.GetAsync(ingredient.ProductId, cancellationToken).ConfigureAwait(false)
                    ?? throw new BadRequestException(ValidationMessages.IngredientIdInvalid);
                products.Add(ingredient.ProductId, product);
            }

            var serving = product.Servings.SingleOrDefault(s => s.ExternalId == mealIngredient.Serving.ServingId)
                ?? throw new BadRequestException(ValidationMessages.IngredientServingIdInvalid);
            calories += serving.Calories * mealIngredient.Quantity;
            carbs += serving.Carbs * mealIngredient.Quantity;
            protein += serving.Protein * mealIngredient.Quantity;
            fat += serving.Fat * mealIngredient.Quantity;
        }

        meal.Calories = calories;
        meal.Carbs = carbs;
        meal.Protein = protein;
        meal.Fat = fat;

        return meal;
    }

    public async Task RecalculateMealsWithIngredientNutrition(Guid ingredientId, CancellationToken cancellationToken = default)
    {
        var meals = await _mealsRepository.GetAll()
            .AsSplitQuery()
            .Include(m => m.Ingredients)
                .ThenInclude(mi => mi.Serving)
            .Include(m => m.NutritionIngredients)
                .ThenInclude(mi => mi.NutritionIngredient)
            .Include(m => m.NutritionIngredients)
                .ThenInclude(mi => mi.Serving)
            .Where(m => m.Deleted == null && m.Ingredients.Any(mi => mi.IngredientId == ingredientId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var meal in meals)
        {
            await RecalculateMealsNutritionAsync(meal, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task UpdateMealsIngredientsAsync(
        Meal meal,
        IEnumerable<IMealIngredientRequest>? ingredients,
        IEnumerable<IMealNutritionIngredientRequest>? nutritionIngredients,
        CancellationToken cancellationToken = default)
    {
        if (ingredients is not null)
        {
            await UpdateMealsPersonalIngredientsAsync(meal, ingredients, cancellationToken).ConfigureAwait(false);
        }
        if (nutritionIngredients is not null)
        {
            await UpdateMealsNutritionIngredientsAsync(meal, nutritionIngredients, cancellationToken).ConfigureAwait(false);
        }
        if (ingredients is not null || nutritionIngredients is not null)
        {
            await RecalculateMealsNutritionAsync(meal, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task UpdateMealsPersonalIngredientsAsync(
        Meal meal,
        IEnumerable<IMealIngredientRequest> ingredients,
        CancellationToken cancellationToken = default)
    {
        var requestIngredients = ingredients.ToList();
        var ingredientLookup = await _ingredientsService
            .GetValidatedIngredientsAsync(requestIngredients, meal.UserId, cancellationToken)
            .ConfigureAwait(false);
        var currentIngredients = meal.Ingredients.ToList();
        var currentByIngredientId = currentIngredients.ToDictionary(mi => mi.IngredientId);
        var requestedIds = requestIngredients.Select(i => i.IngredientId).ToHashSet();

        foreach (var mealIngredient in requestIngredients)
        {
            var ingredient = ingredientLookup[mealIngredient.IngredientId];
            if (currentByIngredientId.TryGetValue(mealIngredient.IngredientId, out var existing))
            {
                existing.ServingId = mealIngredient.ServingId;
                existing.Serving = ingredient.Servings.Single(s => s.Id == mealIngredient.ServingId);
                existing.Quantity = mealIngredient.Quantity;
            }
            else
            {
                meal.Ingredients.Add(new MealIngredient
                {
                    MealId = meal.Id,
                    IngredientId = mealIngredient.IngredientId,
                    ServingId = mealIngredient.ServingId,
                    Serving = ingredient.Servings.Single(s => s.Id == mealIngredient.ServingId),
                    Ingredient = ingredient,
                    Quantity = mealIngredient.Quantity,
                    Created = DateTimeOffset.UtcNow,
                });
            }
        }

        foreach (var removed in currentIngredients.Where(mi => !requestedIds.Contains(mi.IngredientId)).ToList())
        {
            meal.Ingredients.Remove(removed);
        }
    }

    private async Task UpdateMealsNutritionIngredientsAsync(
        Meal meal,
        IEnumerable<IMealNutritionIngredientRequest> ingredients,
        CancellationToken cancellationToken = default)
    {
        var references = ingredients
            .Select(i => new IngredientReference(null, i.ProductId, i.ServingId, i.Quantity))
            .ToList();
        if (references.Count == 0)
        {
            meal.NutritionIngredients.Clear();
            return;
        }

        var resolved = await _ingredientsService.ResolveIngredientsAsync(
            references, meal.UserId, cancellationToken).ConfigureAwait(false);
        var ingredientIds = resolved.Select(i => i.IngredientId).ToList();
        var nutritionIngredientLookup = await _nutritionIngredientsRepository.GetAll()
            .Where(i => ingredientIds.Contains(i.Id))
            .Include(i => i.Servings)
            .ToDictionaryAsync(i => i.Id, cancellationToken)
            .ConfigureAwait(false);

        var currentIngredients = meal.NutritionIngredients.ToList();
        var currentByIngredientId = currentIngredients.ToDictionary(i => i.NutritionIngredientId);
        var requestedIds = ingredientIds.ToHashSet();
        foreach (var ingredient in resolved)
        {
            var identity = nutritionIngredientLookup[ingredient.IngredientId];
            var serving = identity.Servings.Single(s => s.Id == ingredient.ServingId);
            if (currentByIngredientId.TryGetValue(identity.Id, out var existing))
            {
                existing.NutritionIngredient = identity;
                existing.ServingId = ingredient.ServingId;
                existing.Serving = serving;
                existing.Quantity = ingredient.Quantity;
            }
            else
            {
                meal.NutritionIngredients.Add(new MealNutritionIngredient
                {
                    MealId = meal.Id,
                    NutritionIngredientId = identity.Id,
                    NutritionIngredient = identity,
                    ServingId = ingredient.ServingId,
                    Serving = serving,
                    Quantity = ingredient.Quantity,
                    Created = DateTimeOffset.UtcNow,
                });
            }
        }

        foreach (var removed in currentIngredients.Where(i => !requestedIds.Contains(i.NutritionIngredientId)))
        {
            meal.NutritionIngredients.Remove(removed);
        }
    }

    public async Task<Dictionary<Guid, Meal>> GetValidatedMealsAsync(
        IEnumerable<ITreatmentMealRequest> meals,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var requestMeals = meals.ToList();
        var mealIds = requestMeals.Select(m => m.MealId).Distinct().ToList();

        var mealLookup = await _mealsRepository.GetAll()
            .Where(m => mealIds.Contains(m.Id) && m.UserId == userId && m.Deleted == null)
            .ToDictionaryAsync(m => m.Id, cancellationToken)
            .ConfigureAwait(false);

        foreach (var meal in requestMeals)
        {
            if (!mealLookup.ContainsKey(meal.MealId))
            {
                throw new BadRequestException(ValidationMessages.MealIdInvalid);
            }
        }

        return mealLookup;
    }
}
