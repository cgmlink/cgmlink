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

    [TestCase(false)]
    [TestCase(true)]
    public void Should_Accept_Nutrition_Only_Changes_Including_Clearing(bool clear)
    {
        var request = new UpdateTreatmentRequest
        {
            NutritionIngredients = clear ? [] : [new() { ProductId = "product-1", ServingId = "serving-1", Quantity = 1 }],
        };

        _validator.TestValidate(request).ShouldNotHaveAnyValidationErrors();
    }

    [TestCase("", "serving-1", 1, "ProductId")]
    [TestCase("product-1", " ", 1, "ServingId")]
    [TestCase("product-1", "serving-1", 0, "Quantity")]
    [TestCase("product-1", "serving-1", -1, "Quantity")]
    public void Should_Reject_Invalid_Nutrition_Ingredients(string productId, string servingId, int quantity, string property)
    {
        var request = new UpdateTreatmentRequest
        {
            NutritionIngredients = [new() { ProductId = productId, ServingId = servingId, Quantity = quantity }],
        };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor($"NutritionIngredients[0].{property}");
    }

    [Test]
    public void Should_Reject_Duplicate_Nutrition_Products()
    {
        var request = new UpdateTreatmentRequest
        {
            NutritionIngredients =
            [
                new() { ProductId = "product-1", ServingId = "serving-1", Quantity = 1 },
                new() { ProductId = "product-1", ServingId = "serving-2", Quantity = 2 },
            ],
        };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.NutritionIngredients)
            .WithErrorMessage(ValidationMessages.DuplicateIngredientId);
    }

    [Test]
    public void Should_Reject_Null_Nutrition_Ingredient()
    {
        _validator.TestValidate(new UpdateTreatmentRequest { NutritionIngredients = [null!] })
            .ShouldHaveValidationErrorFor("NutritionIngredients[0]");
    }

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
