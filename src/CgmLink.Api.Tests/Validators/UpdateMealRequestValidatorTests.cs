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

    [TestCase(false)]
    [TestCase(true)]
    public void Should_Reject_Null_Nutrition_Ingredient(bool includeValidIngredient)
    {
        var request = new UpdateMealRequest { NutritionIngredients = [null!] };
        if (includeValidIngredient)
        {
            request.NutritionIngredients.Add(new UpdateMealRequest.UpdateMealNutritionIngredientRequest
            {
                ProductId = "product-1",
                ServingId = "serving-1",
                Quantity = 1,
            });
        }

        _validator.TestValidate(request).ShouldHaveValidationErrorFor("NutritionIngredients[0]");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Should_Accept_Nutrition_Only_Update(bool clear)
    {
        var request = new UpdateMealRequest
        {
            NutritionIngredients = clear ? [] : [new UpdateMealRequest.UpdateMealNutritionIngredientRequest
            {
                ProductId = "product-1", ServingId = "serving-1", Quantity = 1,
            }],
        };
        _validator.TestValidate(request).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Should_Accept_Removing_Personal_Ingredients_When_Nutrition_Ingredients_Are_Provided()
    {
        var request = new UpdateMealRequest
        {
            Ingredients = [],
            NutritionIngredients = [new UpdateMealRequest.UpdateMealNutritionIngredientRequest
            {
                ProductId = "product-1", ServingId = "serving-1", Quantity = 1,
            }],
        };
        _validator.TestValidate(request).ShouldNotHaveAnyValidationErrors();
    }

    [TestCase("", "serving-1", 1, "ProductId")]
    [TestCase("product-1", " ", 1, "ServingId")]
    [TestCase("product-1", "serving-1", 0, "Quantity")]
    [TestCase("product-1", "serving-1", -1, "Quantity")]
    public void Should_Reject_Invalid_Nutrition_Ingredient(string productId, string servingId, int quantity, string property)
    {
        var request = new UpdateMealRequest
        {
            NutritionIngredients = [new UpdateMealRequest.UpdateMealNutritionIngredientRequest
            {
                ProductId = productId, ServingId = servingId, Quantity = quantity,
            }],
        };
        _validator.TestValidate(request).ShouldHaveValidationErrorFor($"NutritionIngredients[0].{property}");
    }

    [Test]
    public void Should_Reject_Duplicate_Nutrition_Products()
    {
        var request = new UpdateMealRequest
        {
            NutritionIngredients =
            [
                new UpdateMealRequest.UpdateMealNutritionIngredientRequest
                {
                    ProductId = "product-1", ServingId = "serving-1", Quantity = 1,
                },
                new UpdateMealRequest.UpdateMealNutritionIngredientRequest
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
