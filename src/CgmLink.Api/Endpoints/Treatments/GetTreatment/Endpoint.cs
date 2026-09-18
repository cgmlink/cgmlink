using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Endpoints.Treatments.GetTreatment;

internal static class Endpoint
{
    internal static async Task<Results<Ok<GetTreatmentResponse>, NotFound, UnauthorizedHttpResult>> HandleAsync(
        [FromRoute] Guid id,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IRepository<Treatment> treatmentsRepository,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();

        if (!await treatmentsRepository
                .AnyAsync(t => t.Id == id && t.UserId == userId && t.Deleted == null, cancellationToken)
                .ConfigureAwait(false))
        {
            throw new NotFoundException("TREATMENT_NOT_FOUND");
        }

        var treatment = await treatmentsRepository.GetAll(new FindOptions { IsAsNoTracking = true })
            .Include(t => t.Meals)
                .ThenInclude(tm => tm.Meal)
            .Include(t => t.Ingredients)
                .ThenInclude(ti => ti.Ingredient)
            .Include(t => t.Ingredients)
                .ThenInclude(ti => ti.Serving)
            .Include(t => t.Injection)
                .ThenInclude(i => i.Insulin)
            .Include(t => t.Reading)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId && t.Deleted == null, cancellationToken)
            .ConfigureAwait(false);

        if (treatment is null)
        {
            throw new NotFoundException("TREATMENT_NOT_FOUND");
        }

        return TypedResults.Ok(GetTreatmentResponse.ToResponse(treatment));
    }
}