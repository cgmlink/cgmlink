using CgmLink.Api.Endpoints.Meals.NewMeal;
using CgmLink.Resources;
using FluentValidation.TestHelper;
using NUnit.Framework;
using System;

namespace CgmLink.Api.Tests.Validators;

[TestFixture]
class NewMealRequestValidatorTests
{
    private readonly NewMealRequest.NewMealRequestValidator _validator = new();

    [Test]
    public void Should_Accept_Nutrition_Ingredients_Without_Personal_Ingredients()
    {
        var request = new NewMealRequest
        {
            Name = "Breakfast",
            NutritionIngredients = [new NewMealRequest.NewMealNutritionIngredientRequest
            {
                ProductId = "product-1", ServingId = "serving-1", Quantity = 1,
            }],
        };

        _validator.TestValidate(request).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Should_Reject_Meal_Without_Any_Ingredients()
    {
        _validator.TestValidate(new NewMealRequest { Name = "Breakfast" })
            .ShouldHaveValidationErrorFor(x => x.Ingredients);
    }

    [TestCase("", "serving-1", 1, "ProductId")]
    [TestCase("product-1", " ", 1, "ServingId")]
    [TestCase("product-1", "serving-1", 0, "Quantity")]
    [TestCase("product-1", "serving-1", -1, "Quantity")]
    public void Should_Reject_Invalid_Nutrition_Ingredient(string productId, string servingId, int quantity, string property)
    {
        var request = new NewMealRequest
        {
            Name = "Breakfast",
            NutritionIngredients = [new NewMealRequest.NewMealNutritionIngredientRequest
            {
                ProductId = productId, ServingId = servingId, Quantity = quantity,
            }],
        };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor($"NutritionIngredients[0].{property}");
    }

    [Test]
    public void Should_Reject_Duplicate_Nutrition_Products()
    {
        var request = new NewMealRequest
        {
            Name = "Breakfast",
            NutritionIngredients =
            [
                new NewMealRequest.NewMealNutritionIngredientRequest
                {
                    ProductId = "product-1", ServingId = "serving-1", Quantity = 1,
                },
                new NewMealRequest.NewMealNutritionIngredientRequest
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
        var request = new NewMealRequest
        {
            Name = "Breakfast",
            Ingredients =
            [
                new NewMealRequest.NewMealIngredientRequest
                {
                    IngredientId = ingredientId,
                    ServingId = Guid.NewGuid(),
                    Quantity = 1m,
                },
                new NewMealRequest.NewMealIngredientRequest
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
        var request = new NewMealRequest
        {
            Name = "Breakfast",
            Ingredients =
            [
                new NewMealRequest.NewMealIngredientRequest
                {
                    IngredientId = Guid.NewGuid(),
                    ServingId = Guid.NewGuid(),
                    Quantity = 1m,
                },
                new NewMealRequest.NewMealIngredientRequest
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
}
