using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Identity.Authentication;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Endpoints.Ingredients.NewIngredient;

internal static class Endpoint
{
    internal static async Task<Results<Created<NewIngredientResponse>, ValidationProblem>> HandleAsync(
        [FromBody] NewIngredientRequest request,
        [FromServices] IValidator<NewIngredientRequest> validator,
        [FromServices] IRepository<Ingredient> ingredientsRepository,
        [FromServices] IRepository<User> usersRepository,
        [FromServices] ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false) is
            { IsValid: false } validation)
        {
            return TypedResults.ValidationProblem(validation.ToDictionary());
        }

        var userId = currentUser.GetUserId();
        var user = await usersRepository.FindOneAsync(u => u.Id == userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            throw new UnauthorizedException("USER_NOT_LOGGED_IN", UnauthorizedSource.CgmLink);
        }

        var ingredient = new Ingredient
        {
            Name = request.Name,
            OwnerUserId = userId,
            ImageUrl = request.ImageUrl,
            ThumbnailUrl = request.ThumbnailUrl,
            Created = DateTimeOffset.UtcNow,
        };

        foreach (var servingRequest in request.Servings)
        {
            ingredient.Servings.Add(new IngredientServing
            {
                IngredientId = ingredient.Id,
                Description = servingRequest.Description,
                ServingAmount = servingRequest.ServingAmount,
                ServingUnit = servingRequest.ServingUnit,
                Calories = servingRequest.Calories,
                Carbs = servingRequest.Carbs,
                Protein = servingRequest.Protein,
                Fat = servingRequest.Fat,
                Created = DateTimeOffset.UtcNow,
            });
        }

        await ingredientsRepository.AddAsync(ingredient, cancellationToken).ConfigureAwait(false);

        return TypedResults.Created($"/api/v1/ingredients/{ingredient.Id}", NewIngredientResponse.ToResponse(ingredient));
    }
}
