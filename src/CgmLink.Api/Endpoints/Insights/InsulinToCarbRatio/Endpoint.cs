using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using CgmLink.Api.Models;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CgmLink.Api.Endpoints.Insights.InsulinToCarbRatio;

internal static class Endpoint
{
    internal static async Task<Results<Ok<decimal?>, ValidationProblem, UnauthorizedHttpResult>> HandleAsync(
        [AsParameters] InsulinToCarbRatioRequest request,
        [FromServices] IValidator<InsulinToCarbRatioRequest> validator,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IRepository<Treatment> repository,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();

        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var insulinToCarbs = repository.Find(t =>
                    (int)t.Type == (int)TreatmentType.Meal && t.UserId == userId &&
                    t.Created >= request.From &&
                    t.Created <= request.To,
                new FindOptions { IsAsNoTracking = true }).Include(t => t.Injection).Include(t => t.Ingredients).Include(t => t.Meals)
            .ThenInclude(m => m!.Meal).ThenInclude(m => m!.MealIngredients).ThenInclude(mi => mi.Ingredient)
            .AsEnumerable()
            .Average(x => x.InsulinToCarbRatio);

        return TypedResults.Ok(insulinToCarbs);
    }
}