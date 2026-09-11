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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Endpoints.Ingredients.UpdateIngredient;

internal static class Endpoint
{
    internal static async Task<Results<Ok<UpdateIngredientResponse>, NotFound, UnauthorizedHttpResult, ValidationProblem>> HandleAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateIngredientRequest request,
        [FromServices] IValidator<UpdateIngredientRequest> validator,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IRepository<Ingredient> ingredientsRepository,
        CancellationToken cancellationToken)
    {
        if (await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false) is
            { IsValid: false } validation)
        {
            return TypedResults.ValidationProblem(validation.ToDictionary());
        }

        var userId = currentUser.GetUserId();

        var ingredient = await ingredientsRepository.GetAll()
            .Include(i => i.Servings)
            .FirstOrDefaultAsync(i => i.Id == id && i.Users.Any(u => u.UserId == userId) && i.Deleted == null, cancellationToken)
            .ConfigureAwait(false);

        if (ingredient is null)
        {
            throw new NotFoundException("INGREDIENT_NOT_FOUND");
        }

        if (ingredient.ProductId is not null)
        {
            throw new ConflictException("INGREDIENT_READ_ONLY");
        }

        if (request.Name is not null)
        {
            ingredient.Name = request.Name;
        }
        if (request.Barcode is not null)
        {
            ingredient.Barcode = request.Barcode;
        }
        if (request.ImageUrl is not null)
        {
            ingredient.ImageUrl = request.ImageUrl;
        }
        if (request.ThumbnailUrl is not null)
        {
            ingredient.ThumbnailUrl = request.ThumbnailUrl;
        }
        if (request.Servings is not null)
        {
            ingredient.Servings.Clear();
            foreach (var serving in request.Servings)
            {
                ingredient.Servings.Add(new IngredientServing
                {
                    IngredientId = ingredient.Id,
                    Description = serving.Description,
                    ServingAmount = serving.ServingAmount,
                    ServingUnit = serving.ServingUnit,
                    Calories = serving.Calories,
                    Carbs = serving.Carbs,
                    Protein = serving.Protein,
                    Fat = serving.Fat,
                    Created = DateTimeOffset.UtcNow,
                });
            }
        }
        ingredient.Updated = DateTimeOffset.UtcNow;

        await ingredientsRepository.UpdateAsync(ingredient, cancellationToken).ConfigureAwait(false);

        return TypedResults.Ok(UpdateIngredientResponse.ToResponse(ingredient));
    }
}