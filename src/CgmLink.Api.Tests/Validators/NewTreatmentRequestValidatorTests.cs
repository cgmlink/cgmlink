using CgmLink.Api.Endpoints.Treatments.NewTreatment;
using CgmLink.Resources;
using FluentValidation.TestHelper;
using NUnit.Framework;
using System;

namespace CgmLink.Api.Tests.Validators;

[TestFixture]
class NewTreatmentRequestValidatorTests
{
    private readonly NewTreatmentRequest.NewTreatmentRequestValidator _validator = new();

    [Test]
    public void Should_Accept_Only_Nutrition_Ingredients()
    {
        var request = new NewTreatmentRequest
        {
            NutritionIngredients = [new NewTreatmentRequest.NewTreatmentNutritionIngredientRequest
            {
                ProductId = "product-1", ServingId = "serving-1", Quantity = 1,
            }],
        };

        _validator.TestValidate(request).ShouldNotHaveAnyValidationErrors();
    }

    [TestCase("", "serving-1", 1, "NutritionIngredients[0].ProductId")]
    [TestCase("product-1", "", 1, "NutritionIngredients[0].ServingId")]
    [TestCase("product-1", "serving-1", 0, "NutritionIngredients[0].Quantity")]
    [TestCase("product-1", "serving-1", -1, "NutritionIngredients[0].Quantity")]
    public void Should_Reject_Invalid_Nutrition_Ingredients(string productId, string servingId, int quantity, string property)
    {
        var request = new NewTreatmentRequest
        {
            NutritionIngredients = [new NewTreatmentRequest.NewTreatmentNutritionIngredientRequest
            {
                ProductId = productId, ServingId = servingId, Quantity = quantity,
            }],
        };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(property);
    }

    [Test]
    public void Should_Reject_Null_Nutrition_Ingredients()
    {
        _validator.TestValidate(new NewTreatmentRequest { NutritionIngredients = null! })
            .ShouldHaveValidationErrorFor(x => x.NutritionIngredients);
    }

    [Test]
    public void Should_Reject_Duplicate_Nutrition_Products_With_Different_Servings()
    {
        var request = new NewTreatmentRequest
        {
            NutritionIngredients =
            [
                new NewTreatmentRequest.NewTreatmentNutritionIngredientRequest
                {
                    ProductId = "product-1", ServingId = "serving-1", Quantity = 1,
                },
                new NewTreatmentRequest.NewTreatmentNutritionIngredientRequest
                {
                    ProductId = "product-1", ServingId = "serving-2", Quantity = 2,
                },
            ],
        };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.NutritionIngredients)
            .WithErrorMessage(ValidationMessages.DuplicateIngredientId);
    }

    [Test]
    public void Should_Have_Error_When_Same_Ingredient_Is_Repeated_With_Different_Servings()
    {
        var ingredientId = Guid.NewGuid();
        var request = new NewTreatmentRequest
        {
            Ingredients =
            [
                new NewTreatmentRequest.NewTreatmentIngredientRequest
                {
                    IngredientId = ingredientId,
                    ServingId = Guid.NewGuid(),
                    Quantity = 1m,
                },
                new NewTreatmentRequest.NewTreatmentIngredientRequest
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
    public void Should_Not_Have_Duplicate_Error_When_Ingredients_Are_Distinct()
    {
        var request = new NewTreatmentRequest
        {
            Ingredients =
            [
                new NewTreatmentRequest.NewTreatmentIngredientRequest
                {
                    IngredientId = Guid.NewGuid(),
                    ServingId = Guid.NewGuid(),
                    Quantity = 1m,
                },
                new NewTreatmentRequest.NewTreatmentIngredientRequest
                {
                    IngredientId = Guid.NewGuid(),
                    ServingId = Guid.NewGuid(),
                    Quantity = 1m,
                },
            ],
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Ingredients);
    }

    [Test]
    public void Should_Have_Error_When_Same_Meal_Is_Repeated()
    {
        var mealId = Guid.NewGuid();
        var request = new NewTreatmentRequest
        {
            Meals =
            [
                new NewTreatmentRequest.NewTreatmentMealRequest { MealId = mealId, Quantity = 1m },
                new NewTreatmentRequest.NewTreatmentMealRequest { MealId = mealId, Quantity = 2m },
            ],
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Meals)
            .WithErrorMessage(ValidationMessages.DuplicateMealId);
    }

    [Test]
    public void Should_Not_Have_Duplicate_Error_When_Meals_Are_Distinct()
    {
        var request = new NewTreatmentRequest
        {
            Meals =
            [
                new NewTreatmentRequest.NewTreatmentMealRequest { MealId = Guid.NewGuid(), Quantity = 1m },
                new NewTreatmentRequest.NewTreatmentMealRequest { MealId = Guid.NewGuid(), Quantity = 1m },
            ],
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Meals);
    }
}
