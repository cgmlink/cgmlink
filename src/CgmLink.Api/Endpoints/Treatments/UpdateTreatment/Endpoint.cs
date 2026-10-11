using CgmLink.Api.Services;
using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Identity.Authentication;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Endpoints.Treatments.UpdateTreatment;

internal static class Endpoint
{
    internal static async Task<Results<Ok<UpdateTreatmentResponse>, NotFound, UnauthorizedHttpResult, ValidationProblem>> HandleAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateTreatmentRequest request,
        [FromServices] IValidator<UpdateTreatmentRequest> validator,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IRepository<Treatment> treatmentsRepository,
        [FromServices] IRepository<Reading> readingsRepository,
        [FromServices] IRepository<Insulin> insulinsRepository,
        [FromServices] ITreatmentService treatmentService,
        CancellationToken cancellationToken)
    {
        if (await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false) is
            { IsValid: false } validation)
        {
            return TypedResults.ValidationProblem(validation.ToDictionary());
        }

        var userId = currentUser.GetUserId();

        if (!await treatmentsRepository
                .AnyAsync(t => t.Id == id && t.UserId == userId && t.Deleted == null, cancellationToken)
                .ConfigureAwait(false))
        {
            throw new NotFoundException("TREATMENT_NOT_FOUND");
        }

        var treatment = await treatmentsRepository.GetAll()
            .Include(t => t.Meals)
                .ThenInclude(tm => tm.Meal)
            .Include(t => t.Ingredients)
                .ThenInclude(ti => ti.Ingredient)
            .Include(t => t.Ingredients)
                .ThenInclude(ti => ti.Serving)
            .Include(t => t.NutritionIngredients)
            .Include(t => t.Injection)
                .ThenInclude(i => i.Insulin)
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (treatment is null)
        {
            throw new NotFoundException("TREATMENT_NOT_FOUND");
        }

        var updated = DateTimeOffset.UtcNow;

        var injection = treatment.Injection;

        var injectionRequest = request.Injection;

        if (injectionRequest is not null)
        {
            var insulin = await insulinsRepository
                .FindOneAsync(i => i.Id == injectionRequest.InsulinId && (i.UserId == userId || i.UserId == null),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (insulin is null)
            {
                throw new NotFoundException("INSULIN_NOT_FOUND");
            }

            if (injection is null)
            {
                injection = new Injection
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    InsulinId = insulin.Id,
                    Units = injectionRequest.Units,
                    Created = updated,
                    Insulin = insulin,
                };
            }
            else
            {
                injection.InsulinId = insulin.Id;
                injection.Units = injectionRequest.Units;
                injection.Updated = updated;
                injection.Insulin = insulin;
            }
        }

        if (request.ReadingId is not null)
        {
            var reading = await readingsRepository
                .FindOneAsync(r => r.Id == request.ReadingId && r.UserId == userId,
                    new FindOptions { IsAsNoTracking = true }, cancellationToken)
                .ConfigureAwait(false);

            if (reading is null)
            {
                throw new NotFoundException("READING_NOT_FOUND");
            }
        }

        if (injection is not null)
        {
            treatment.Injection = injection;
            treatment.InjectionId = injection.Id;
        }

        if (request.ReadingId is not null)
        {
            treatment.ReadingId = request.ReadingId;
        }

        await treatmentService.UpdateTreatmentFoodsAsync(
            treatment,
            request.Meals,
            request.Ingredients,
            request.NutritionIngredients?.Select(IngredientReference.FromNutrition),
            updated,
            cancellationToken).ConfigureAwait(false);

        treatment.Updated = updated;

        await treatmentsRepository.UpdateAsync(treatment, cancellationToken).ConfigureAwait(false);

        return TypedResults.Ok(UpdateTreatmentResponse.ToResponse(treatment));
    }
}
