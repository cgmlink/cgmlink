using System;
using CgmLink.Api.Endpoints.Meals.UpdateMeal;
using FluentValidation.TestHelper;
using NUnit.Framework;

namespace CgmLink.Api.Tests.Validators;

[TestFixture]
class UpdateMealRequestValidatorTests
{
    private readonly UpdateMealRequest.UpdateMealRequestValidator _validator = new();

    [Test]
    public void Should_Have_Error_When_All_Properties_Are_Null()
    {
        var request = new UpdateMealRequest();
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x);
    }

    [Test]
    public void Should_Not_Have_Error_When_Name_Is_Provided()
    {
        var request = new UpdateMealRequest { Name = "Breakfast" };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    [Test]
    public void Should_Not_Have_Error_When_ImageUrl_Is_Provided()
    {
        var request = new UpdateMealRequest { ImageUrl = "https://example.com/image.jpg" };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    [Test]
    public void Should_Not_Have_Error_When_ThumbnailUrl_Is_Provided()
    {
        var request = new UpdateMealRequest { ThumbnailUrl = "https://example.com/thumb.jpg" };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    [Test]
    public void Should_Not_Have_Error_When_Ingredients_Are_Provided()
    {
        var request = new UpdateMealRequest
        {
            Ingredients =
            [
                new UpdateMealRequest.UpdateMealIngredientRequest
                {
                    IngredientId = Guid.NewGuid(),
                    ServingId = Guid.NewGuid(),
                    Quantity = 1m,
                }
            ]
        };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    [Test]
    public void Should_Have_Error_When_Name_Is_Empty()
    {
        var request = new UpdateMealRequest { Name = "" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Should_Have_Error_When_Ingredients_Is_Empty()
    {
        var request = new UpdateMealRequest
        {
            Name = "Breakfast",
            Ingredients = [],
        };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Ingredients);
    }

    [Test]
    public void Should_Have_Error_When_Ingredient_Quantity_Is_Zero_Or_Less()
    {
        var request = new UpdateMealRequest
        {
            Ingredients =
            [
                new UpdateMealRequest.UpdateMealIngredientRequest
                {
                    IngredientId = Guid.NewGuid(),
                    ServingId = Guid.NewGuid(),
                    Quantity = 0m,
                }
            ]
        };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("Ingredients[0].Quantity");
    }

    [Test]
    public void Should_Have_Error_When_IngredientId_Is_Empty()
    {
        var request = new UpdateMealRequest
        {
            Ingredients =
            [
                new UpdateMealRequest.UpdateMealIngredientRequest
                {
                    IngredientId = Guid.Empty,
                    ServingId = Guid.NewGuid(),
                    Quantity = 1m,
                }
            ]
        };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("Ingredients[0].IngredientId");
    }

    [Test]
    public void Should_Have_Error_When_ServingId_Is_Empty()
    {
        var request = new UpdateMealRequest
        {
            Ingredients =
            [
                new UpdateMealRequest.UpdateMealIngredientRequest
                {
                    IngredientId = Guid.NewGuid(),
                    ServingId = Guid.Empty,
                    Quantity = 1m,
                }
            ]
        };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("Ingredients[0].ServingId");
    }
}