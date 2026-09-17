using CgmLink.Api.Endpoints.Treatments.UpdateTreatment;
using CgmLink.Resources;
using FluentValidation.TestHelper;
using NUnit.Framework;
using System;

namespace CgmLink.Api.Tests.Validators;

[TestFixture]
class UpdateTreatmentRequestValidatorTests
{
    private readonly UpdateTreatmentRequest.UpdateTreatmentRequestValidator _validator = new();

    [Test]
    public void Should_Have_Error_When_Same_Ingredient_Is_Repeated_With_Different_Servings()
    {
        var ingredientId = Guid.NewGuid();
        var request = new UpdateTreatmentRequest
        {
            Ingredients =
            [
                new UpdateTreatmentRequest.UpdateTreatmentIngredientRequest
                {
                    IngredientId = ingredientId,
                    ServingId = Guid.NewGuid(),
                    Quantity = 1m,
                },
                new UpdateTreatmentRequest.UpdateTreatmentIngredientRequest
                {
                    IngredientId = ingredientId,
                    ServingId = Guid.NewGuid(),
                    Quantity = 2m,
                },
            ],
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Ingredients)
            .WithErrorMessage(ValidationMessages.DuplicateIngredientId);
    }

    [Test]
    public void Should_Not_Have_Error_When_Ingredients_Are_Not_Provided()
    {
        var request = new UpdateTreatmentRequest { ReadingId = Guid.NewGuid() };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Ingredients);
    }

    [Test]
    public void Should_Have_Error_When_Same_Meal_Is_Repeated()
    {
        var mealId = Guid.NewGuid();
        var request = new UpdateTreatmentRequest
        {
            Meals =
            [
                new UpdateTreatmentRequest.UpdateTreatmentMealRequest { MealId = mealId, Quantity = 1m },
                new UpdateTreatmentRequest.UpdateTreatmentMealRequest { MealId = mealId, Quantity = 2m },
            ],
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Meals)
            .WithErrorMessage(ValidationMessages.DuplicateMealId);
    }

    [Test]
    public void Should_Not_Have_Error_When_Meals_Are_Not_Provided()
    {
        var request = new UpdateTreatmentRequest { ReadingId = Guid.NewGuid() };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Meals);
    }
}
