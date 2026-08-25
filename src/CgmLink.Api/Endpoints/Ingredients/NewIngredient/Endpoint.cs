using FluentValidation;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Data.Enums;

namespace CgmLink.Api.Endpoints.Ingredients.NewIngredient;

internal static class Endpoint
{
    internal static async Task<Results<Ok<NewIngredientResponse>, Conflict<NewIngredientResponse>, ValidationProblem>> HandleAsync(
        [FromBody] NewIngredientRequest request,
        [FromServices] IValidator<NewIngredientRequest> validator,
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

        if (!string.IsNullOrEmpty(request.Barcode))
        {
            var existingIngredient = await ingredientRepository
                .FindOneAsync(i => i.UserId == userId && i.Barcode == request.Barcode, new FindOptions { IsAsNoTracking = true, IsIgnoreAutoIncludes = true }, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (existingIngredient is not null)
            {
                var existingBrands = await brandRepository
                    .Find(b => b.IngredientId == existingIngredient.Id, new FindOptions { IsAsNoTracking = true, IsIgnoreAutoIncludes = true })
                    .Select(b => b.Name)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                var conflictResponse = new NewIngredientResponse
                {
                    Id = existingIngredient.Id,
                    Barcode = existingIngredient.Barcode,
                    ImageUrl = existingIngredient.ImageUrl,
                    ThumbnailUrl = existingIngredient.ThumbnailUrl,
                    Brands = existingBrands,
                    Created = existingIngredient.Created,
                    Name = existingIngredient.Name,
                    Carbs = existingIngredient.Carbs,
                    Protein = existingIngredient.Protein,
                    Fat = existingIngredient.Fat,
                    Calories = existingIngredient.Calories,
                    Uom = (Models.UnitOfMeasurement)existingIngredient.Uom,
                    Updated = existingIngredient.Updated,
                };

                return TypedResults.Conflict(conflictResponse);
            }
        }

        var ingredient = new Ingredient
        {
            Barcode = request.Barcode,
            ImageUrl = request.ImageUrl,
            ThumbnailUrl = request.ThumbnailUrl,
            Name = request.Name,
            Created = DateTimeOffset.UtcNow,
            Carbs = request.Carbs,
            Protein = request.Protein,
            Fat = request.Fat,
            Calories = request.Calories,
            Uom = (UnitOfMeasurement)request.Uom,
            UserId = userId,
        };

        await ingredientRepository.AddAsync(ingredient, cancellationToken).ConfigureAwait(false);

        var brandNames = request.Brands is { Count: > 0 }
            ? request.Brands.Where(b => !string.IsNullOrWhiteSpace(b)).Distinct().ToList()
            : [];

        if (brandNames.Count > 0)
        {
            await brandRepository
                .AddManyAsync(brandNames.Select(name => new IngredientBrand { IngredientId = ingredient.Id, Name = name }), cancellationToken)
                .ConfigureAwait(false);
        }

        var response = new NewIngredientResponse
        {
            Id = ingredient.Id,
            Barcode = ingredient.Barcode,
            ImageUrl = ingredient.ImageUrl,
            ThumbnailUrl = ingredient.ThumbnailUrl,
            Brands = brandNames,
            Created = ingredient.Created,
            Name = ingredient.Name,
            Carbs = ingredient.Carbs,
            Protein = ingredient.Protein,
            Fat = ingredient.Fat,
            Calories = ingredient.Calories,
            Uom = (Models.UnitOfMeasurement)ingredient.Uom,
        };

        return TypedResults.Ok(response);
    }
}
