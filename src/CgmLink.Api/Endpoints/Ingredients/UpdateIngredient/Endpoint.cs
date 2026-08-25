using FluentValidation;
using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Api.Models;

namespace CgmLink.Api.Endpoints.Ingredients.UpdateIngredient;

internal static class Endpoint
{
    internal static async Task<Results<Ok<UpdateIngredientResponse>, NotFound, UnauthorizedHttpResult, ValidationProblem>> HandleAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateIngredientRequest request,
        [FromServices] IValidator<UpdateIngredientRequest> validator,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IRepository<Ingredient> ingredientRepository,
        [FromServices] IRepository<IngredientBrand> brandRepository,
        CancellationToken cancellationToken)
    {
        if (await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false) is
            { IsValid: false } validation)
        {
            return TypedResults.ValidationProblem(validation.ToDictionary());
        }

        var userId = currentUser.GetUserId();

        var ingredient = await ingredientRepository.FindOneAsync(i => i.Id == id && i.UserId == userId, new FindOptions() { IsAsNoTracking = false }, cancellationToken).ConfigureAwait(false);

        if (ingredient is null)
        {
            throw new NotFoundException("INGREDIENT_NOT_FOUND");
        }

        if (request.Barcode is not null)
        {
            ingredient.Barcode = request.Barcode;
        }
        ingredient.Name = request.Name;
        ingredient.Carbs = request.Carbs;
        ingredient.Protein = request.Protein;
        ingredient.Fat = request.Fat;
        ingredient.Calories = request.Calories;
        ingredient.Uom = (Data.Enums.UnitOfMeasurement)request.Uom;
        ingredient.Updated = DateTimeOffset.UtcNow;

        await ingredientRepository.UpdateAsync(ingredient, cancellationToken).ConfigureAwait(false);

        List<string> brandNames;
        if (request.Brands is not null)
        {
            await brandRepository.DeleteManyAsync(b => b.IngredientId == ingredient.Id, cancellationToken).ConfigureAwait(false);

            brandNames = request.Brands.Where(b => !string.IsNullOrWhiteSpace(b)).Distinct().ToList();
            if (brandNames.Count > 0)
            {
                await brandRepository
                    .AddManyAsync(brandNames.Select(name => new IngredientBrand { IngredientId = ingredient.Id, Name = name }), cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        else
        {
            brandNames = await brandRepository
                .Find(b => b.IngredientId == ingredient.Id, new FindOptions { IsAsNoTracking = true, IsIgnoreAutoIncludes = true })
                .Select(b => b.Name)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        var response = new UpdateIngredientResponse
        {
            Id = ingredient.Id,
            Barcode = ingredient.Barcode,
            Brands = brandNames,
            Name = ingredient.Name,
            Carbs = ingredient.Carbs,
            Protein = ingredient.Protein,
            Fat = ingredient.Fat,
            Calories = ingredient.Calories,
            Uom = (UnitOfMeasurement)ingredient.Uom,
            Updated = ingredient.Updated
        };

        return TypedResults.Ok(response);
    }
}
