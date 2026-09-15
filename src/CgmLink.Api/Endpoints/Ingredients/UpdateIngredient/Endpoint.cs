using FluentValidation;
using CgmLink.AspNetCore.Exceptions;
using CgmLink.Api.Services;
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
        [FromServices] IMealService mealService,
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
            var existingServings = ingredient.Servings.ToList();
            var requestedServingIds = request.Servings
                .Where(s => s.Id is not null)
                .Select(s => s.Id!.Value)
                .ToHashSet();

            foreach (var servingRequest in request.Servings)
            {
                var existing = servingRequest.Id is { } servingId
                    ? existingServings.FirstOrDefault(s => s.Id == servingId)
                    : null;

                if (existing is not null)
                {
                    existing.Description = servingRequest.Description;
                    existing.ServingAmount = servingRequest.ServingAmount;
                    existing.ServingUnit = servingRequest.ServingUnit;
                    existing.Calories = servingRequest.Calories;
                    existing.Carbs = servingRequest.Carbs;
                    existing.Protein = servingRequest.Protein;
                    existing.Fat = servingRequest.Fat;
                    existing.Deleted = null;
                }
                else
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
            }

            var deletedAt = DateTimeOffset.UtcNow;
            foreach (var removed in existingServings.Where(s => s.Deleted == null && !requestedServingIds.Contains(s.Id)))
            {
                removed.Deleted = deletedAt;
            }

            await mealService.RecalculateMealsWithIngredientNutrition(ingredient.Id, cancellationToken).ConfigureAwait(false);
        }
        ingredient.Updated = DateTimeOffset.UtcNow;

        await ingredientsRepository.UpdateAsync(ingredient, cancellationToken).ConfigureAwait(false);

        return TypedResults.Ok(UpdateIngredientResponse.ToResponse(ingredient));
    }
}