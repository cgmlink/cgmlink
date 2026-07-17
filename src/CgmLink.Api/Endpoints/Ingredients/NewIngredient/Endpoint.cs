using FluentValidation;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System;
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
                var conflictResponse = new NewIngredientResponse
                {
                    Id = existingIngredient.Id,
                    Barcode = existingIngredient.Barcode,
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

        var response = new NewIngredientResponse
        {
            Id = ingredient.Id,
            Barcode = ingredient.Barcode,
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
