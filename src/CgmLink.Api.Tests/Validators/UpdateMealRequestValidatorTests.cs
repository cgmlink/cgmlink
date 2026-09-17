using CgmLink.Api.Endpoints.Meals.UpdateMeal;
using CgmLink.Resources;
using FluentValidation.TestHelper;
using NUnit.Framework;
using System;

namespace CgmLink.Api.Tests.Validators;

[TestFixture]
class UpdateMealRequestValidatorTests
{
    private readonly UpdateMealRequest.UpdateMealRequestValidator _validator = new();

    [Test]
    public void Should_Have_Error_When_Same_Ingredient_Is_Repeated_With_Different_Servings()
    {
        var ingredientId = Guid.NewGuid();
        var request = new UpdateMealRequest
        {
            Ingredients =
            [
                new UpdateMealRequest.UpdateMealIngredientRequest
                {
                    IngredientId = ingredientId,
                    ServingId = Guid.NewGuid(),
                    Quantity = 1m,
                },
                new UpdateMealRequest.UpdateMealIngredientRequest
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
        var request = new UpdateMealRequest { Name = "Breakfast" };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Ingredients);
    }
}
