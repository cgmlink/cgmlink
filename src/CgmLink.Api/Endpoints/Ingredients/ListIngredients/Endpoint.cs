using FluentValidation;
using CgmLink.Api.Models;
using CgmLink.Data.Entities;
using CgmLink.Data.Extensions;
using CgmLink.Data.Repository;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Endpoints.Ingredients.ListIngredients;

internal static class Endpoint
{
    internal static async Task<Results<Ok<ListIngredientsResponse>, UnauthorizedHttpResult, ValidationProblem>> HandleAsync(
        [AsParameters] ListIngredientsRequest request,
        [FromServices] IValidator<ListIngredientsRequest> validator,
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
        var sortBy = string.IsNullOrWhiteSpace(request.SortBy) ? nameof(Ingredient.Created) : request.SortBy;
        var descending = request.SortDirection == SortDirection.Desc;

        var ingredients = ingredientsRepository.Find(i => i.Users.Any(u => u.UserId == userId) && i.Deleted == null, new FindOptions { IsAsNoTracking = true })
            .OrderByProperty(sortBy, descending)
            .Skip(request.Page * request.PageSize)
            .Take(request.PageSize)
            .Select(i => new GetIngredientResponse
            {
                Id = i.Id,
                Name = i.Name,
                Barcode = i.Barcode,
                ProductId = i.ProductId,
                ImageUrl = i.ImageUrl,
                ThumbnailUrl = i.ThumbnailUrl,
                Created = i.Created,
                Updated = i.Updated,
            })
            .ToList();

        var totalIngredients = await ingredientsRepository.CountAsync(i => i.Users.Any(u => u.UserId == userId) && i.Deleted == null, cancellationToken).ConfigureAwait(false);
        var numberOfPages = (int)Math.Ceiling(totalIngredients / (double)request.PageSize);

        var response = new ListIngredientsResponse
        {
            Ingredients = ingredients,
            NumberOfPages = numberOfPages,
        };

        return TypedResults.Ok(response);
    }
}