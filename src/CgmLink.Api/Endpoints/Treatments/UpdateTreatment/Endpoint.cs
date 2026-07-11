using FluentValidation;
using CgmLink.Api.Endpoints.Treatments.NewTreatment;
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

namespace CgmLink.Api.Endpoints.Treatments.UpdateTreatment;

internal static class Endpoint
{
    internal static async Task<Results<Ok<UpdateTreatmentResponse>, NotFound, UnauthorizedHttpResult, ValidationProblem>> HandleAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateTreatmentRequest request,
        [FromServices] IValidator<UpdateTreatmentRequest> validator,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IRepository<Treatment> treatmentRepository,
        [FromServices] IRepository<Reading> readingRepository,
        [FromServices] IRepository<Meal> mealRepository,
        [FromServices] IRepository<Ingredient> ingredientRepository,
        [FromServices] IRepository<Injection> injectionRepository,
        CancellationToken cancellationToken)
    {
        if (await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false) is
            { IsValid: false } validation)
        {
            return TypedResults.ValidationProblem(validation.ToDictionary());
        }

        var userId = currentUser.GetUserId();

        var treatment = treatmentRepository
            .Find(t => t.Id == id && t.UserId == userId, new FindOptions { IsAsNoTracking = false })
            .Include(i => i.Injection)
            .Include(r => r.Reading)
            .Include(t => t.Meals)
            .Include(t => t.Ingredients)
            .AsSplitQuery()
            .FirstOrDefault();

        if (treatment is null)
        {
            throw new NotFoundException("TREATMENT_NOT_FOUND");
        }

        Injection? injection = null;

        if (request.Injection?.Id is not null)
        {
            injection = injectionRepository
            .Find(i => i.Id == request.Injection.Id && i.UserId == userId, new FindOptions { IsAsNoTracking = true })
            .Include(i => i.Insulin).FirstOrDefault();

            if (injection is null)
            {
                throw new NotFoundException("INJECTION_NOT_FOUND");
            }

            injection.Updated = DateTimeOffset.UtcNow;
            injection.InsulinId = request.Injection.InsulinId;
            injection.Units = request.Injection.Units;
        }
        else if (request.Injection is not null)
        {
            injection = new Injection
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                InsulinId = request.Injection.InsulinId,
                Units = request.Injection.Units,
                Created = DateTimeOffset.UtcNow,
            };
        }

        treatment.Injection = injection;

        var mealIds = request.Meals.Select(m => m.Id).ToList();
        var meals = mealRepository.Find(m => mealIds.Contains(m.Id) && m.UserId == userId, new FindOptions { IsAsNoTracking = true }).ToList();
        var invalidMealIds = mealIds.Except(meals.Select(m => m.Id)).ToList();

        if (invalidMealIds.Count > 0)
        {
            throw new NotFoundException("MEAL_NOT_FOUND");
        }

        var ingredientIds = request.Ingredients.Select(i => i.Id).ToList();
        var ingredients = ingredientRepository.Find(i => ingredientIds.Contains(i.Id) && i.UserId == userId, new FindOptions { IsAsNoTracking = true }).ToList();
        var invalidIngredientIds = ingredientIds.Except(ingredients.Select(i => i.Id)).ToList();

        if (invalidIngredientIds.Count > 0)
        {
            throw new NotFoundException("INGREDIENT_NOT_FOUND");
        }

        Reading? reading = null;
        if (request.ReadingId is not null)
        {
            reading = await readingRepository
                .FindOneAsync(r => r.Id == request.ReadingId && r.UserId == userId, new FindOptions { IsAsNoTracking = true }, cancellationToken)
                .ConfigureAwait(false);
            if (reading is null)
            {
                throw new NotFoundException("READING_NOT_FOUND");
            }
        }

        treatment.ReadingId = request.ReadingId;

        if (request.Created is not null)
        {
            treatment.Created = request.Created.Value;
        }

        treatment.Meals = request.Meals.Select(m => new TreatmentMeal
        {
            Id = Guid.NewGuid(),
            MealId = m.Id,
            TreatmentId = treatment.Id,
            Quantity = m.Quantity,
        }).ToList();
        treatment.Ingredients = request.Ingredients.Select(i => new TreatmentIngredient
        {
            Id = Guid.NewGuid(),
            IngredientId = i.Id,
            TreatmentId = treatment.Id,
            Quantity = i.Quantity,
        }).ToList();
        treatment.Updated = DateTimeOffset.UtcNow;

        await treatmentRepository.UpdateAsync(treatment, cancellationToken);

        var response = new UpdateTreatmentResponse
        {
            Id = treatment.Id,
            ReadingId = treatment.ReadingId,
            ReadingGlucoseLevel = reading?.GlucoseLevel,
            Type = (Models.TreatmentType)treatment.Type,

            Meals = treatment.Meals.Select(m => new UpdateTreatmentMealResponse
            {
                Id = m.MealId,
                Name = m.Meal?.Name ?? "",
                Quantity = m.Quantity,
            }).ToList(),
            Ingredients = treatment.Ingredients.Select(i => new UpdateTreatmentIngredientResponse
            {
                Id = i.IngredientId,
                Name = i.Ingredient?.Name ?? "",
                Quantity = i.Quantity,
            }).ToList(),
            InjectionId = treatment.InjectionId,
            InsulinId = injection?.Insulin?.Id,
            InsulinName = injection?.Insulin?.Name,
            InsulinUnits = injection?.Units,
            Created = treatment.Created,
            Updated = treatment.Updated
        };

        return TypedResults.Ok(response);
    }
}
