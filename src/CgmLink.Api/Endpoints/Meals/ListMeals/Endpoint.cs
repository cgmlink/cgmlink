using FluentValidation;
using CgmLink.Api.Endpoints.Meals.GetMeal;
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

namespace CgmLink.Api.Endpoints.Meals.ListMeals;

internal static class Endpoint
{
    internal static async Task<Results<Ok<ListMealsResponse>, UnauthorizedHttpResult, ValidationProblem>> HandleAsync(
        [AsParameters] ListMealsRequest request,
        [FromServices] IValidator<ListMealsRequest> validator,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IRepository<Meal> mealsRepository,
        CancellationToken cancellationToken)
    {
        if (await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false) is
            { IsValid: false } validation)
        {
            return TypedResults.ValidationProblem(validation.ToDictionary());
        }

        var userId = currentUser.GetUserId();
        var sortBy = string.IsNullOrWhiteSpace(request.SortBy) ? nameof(Meal.Created) : request.SortBy;
        var descending = request.SortDirection == SortDirection.Desc;

        var meals = mealsRepository.Find(m => m.UserId == userId && m.Deleted == null, new FindOptions { IsAsNoTracking = true })
            .OrderByProperty(sortBy, descending)
            .Skip(request.Page * request.PageSize)
            .Take(request.PageSize)
            .Select(m => GetMealResponse.ToResponse(m, m.Ingredients.Count))
            .ToList();

        var totalMeals = await mealsRepository.CountAsync(m => m.UserId == userId && m.Deleted == null, cancellationToken).ConfigureAwait(false);
        var numberOfPages = (int)Math.Ceiling(totalMeals / (double)request.PageSize);

        var response = new ListMealsResponse
        {
            Meals = meals,
            NumberOfPages = numberOfPages,
        };

        return TypedResults.Ok(response);
    }
}