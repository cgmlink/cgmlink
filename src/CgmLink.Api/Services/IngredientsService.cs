using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Resources;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Services;

public sealed class IngredientsService : IIngredientsService
{
    private readonly IRepository<Ingredient> _ingredientsRepository;

    public IngredientsService(IRepository<Ingredient> ingredientsRepository)
    {
        _ingredientsRepository = ingredientsRepository;
    }

    public async Task<Dictionary<Guid, Ingredient>> GetValidatedIngredientsAsync(
        IEnumerable<IMealIngredientRequest> ingredients,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var requestIngredients = ingredients.ToList();
        var ingredientIds = requestIngredients.Select(i => i.IngredientId).Distinct().ToList();
        var servingIds = requestIngredients.Select(i => i.ServingId).Distinct().ToList();

        var ingredientLookup = await _ingredientsRepository.GetAll()
            .Where(i => ingredientIds.Contains(i.Id) && i.Users.Any(u => u.UserId == userId) && i.Deleted == null)
            .Include(i => i.Servings.Where(s => servingIds.Contains(s.Id) && s.Deleted == null))
            .ToDictionaryAsync(i => i.Id, cancellationToken)
            .ConfigureAwait(false);

        foreach (var mealIngredient in requestIngredients)
        {
            if (!ingredientLookup.TryGetValue(mealIngredient.IngredientId, out var ingredient) ||
                !ingredient.Servings.Any(s => s.Id == mealIngredient.ServingId))
            {
                throw new BadRequestException(ValidationMessages.IngredientIdInvalid);
            }
        }

        return ingredientLookup;
    }
}