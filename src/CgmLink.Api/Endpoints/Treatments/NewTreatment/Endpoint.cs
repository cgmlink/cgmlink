using CgmLink.Api.Services;
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

namespace CgmLink.Api.Endpoints.Treatments.NewTreatment;

internal static class Endpoint
{
    internal static async Task<Results<Created<NewTreatmentResponse>, ValidationProblem>> HandleAsync(
        [FromBody] NewTreatmentRequest request,
        [FromServices] IValidator<NewTreatmentRequest> validator,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IRepository<User> usersRepository,
        [FromServices] IRepository<Reading> readingsRepository,
        [FromServices] IRepository<Insulin> insulinsRepository,
        [FromServices] IRepository<Injection> injectionsRepository,
        [FromServices] IRepository<Treatment> treatmentsRepository,
        [FromServices] IMealService mealService,
        [FromServices] IIngredientsService ingredientsService,
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

        var created = request.Created ?? DateTimeOffset.UtcNow;

        Injection? injection = null;

        if (request.Injection is not null)
        {
            var insulin = await insulinsRepository
                .FindOneAsync(i => i.Id == request.Injection.InsulinId && (i.UserId == userId || i.UserId == null),
                    new FindOptions { IsAsNoTracking = true }, cancellationToken)
                .ConfigureAwait(false);

            if (insulin is null)
            {
                throw new NotFoundException("INSULIN_NOT_FOUND");
            }

            injection = new Injection
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                InsulinId = request.Injection.InsulinId,
                Units = request.Injection.Units,
                Created = created,
                Insulin = insulin,
            };
        }

        var mealLookup = await mealService.GetValidatedMealsAsync(request.Meals, userId, cancellationToken).ConfigureAwait(false);
        var ingredientLookup = await ingredientsService
            .GetValidatedIngredientsAsync(request.Ingredients, userId, cancellationToken)
            .ConfigureAwait(false);

        Reading? reading = null;

        if (request.ReadingId is not null)
        {
            reading = await readingsRepository
                .FindOneAsync(r => r.Id == request.ReadingId && r.UserId == userId,
                    new FindOptions { IsAsNoTracking = true }, cancellationToken)
                .ConfigureAwait(false);

            if (reading is null)
            {
                throw new NotFoundException("READING_NOT_FOUND");
            }
        }

        var calories = 0m;
        var carbs = 0m;
        var protein = 0m;
        var fat = 0m;

        foreach (var mealRequest in request.Meals)
        {
            var meal = mealLookup[mealRequest.MealId];
            calories += meal.Calories * mealRequest.Quantity;
            carbs += meal.Carbs * mealRequest.Quantity;
            protein += meal.Protein * mealRequest.Quantity;
            fat += meal.Fat * mealRequest.Quantity;
        }

        foreach (var ingredientRequest in request.Ingredients)
        {
            var ingredient = ingredientLookup[ingredientRequest.IngredientId];
            var serving = ingredient.Servings.Single(s => s.Id == ingredientRequest.ServingId);
            calories += serving.Calories * ingredientRequest.Quantity;
            carbs += serving.Carbs * ingredientRequest.Quantity;
            protein += serving.Protein * ingredientRequest.Quantity;
            fat += serving.Fat * ingredientRequest.Quantity;
        }

        var treatmentId = Guid.NewGuid();

        if (injection is not null)
        {
            await injectionsRepository.AddAsync(injection, cancellationToken).ConfigureAwait(false);
        }

        var treatment = new Treatment
        {
            Id = treatmentId,
            UserId = userId,
            ReadingId = reading?.Id,
            InjectionId = injection?.Id,
            Calories = calories,
            Carbs = carbs,
            Protein = protein,
            Fat = fat,
            Created = created,
            Meals = request.Meals.Select(m => new TreatmentMeal
            {
                Id = Guid.NewGuid(),
                TreatmentId = treatmentId,
                MealId = m.MealId,
                Quantity = m.Quantity,
                Created = created,
            }).ToList(),
            Ingredients = request.Ingredients.Select(i => new TreatmentIngredient
            {
                Id = Guid.NewGuid(),
                TreatmentId = treatmentId,
                IngredientId = i.IngredientId,
                ServingId = i.ServingId,
                Quantity = i.Quantity,
                Created = created,
            }).ToList(),
        };

        await treatmentsRepository.AddAsync(treatment, cancellationToken).ConfigureAwait(false);

        return TypedResults.Created($"/api/v1/treatments/{treatment.Id}", NewTreatmentResponse.ToResponse(treatment, injection));
    }
}