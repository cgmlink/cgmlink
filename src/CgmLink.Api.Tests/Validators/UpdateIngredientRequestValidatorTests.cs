using CgmLink.Api.Endpoints.Ingredients.UpdateIngredient;
using FluentValidation.TestHelper;
using NUnit.Framework;

namespace CgmLink.Api.Tests.Validators;

[TestFixture]
class UpdateIngredientRequestValidatorTests
{
    private readonly UpdateIngredientRequest.UpdateIngredientRequestValidator _validator = new();

    [Test]
    public void Should_Have_Error_When_All_Properties_Are_Null()
    {
        var request = new UpdateIngredientRequest();
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x);
    }

    [Test]
    public void Should_Not_Have_Error_When_Name_Is_Provided()
    {
        var request = new UpdateIngredientRequest { Name = "Milk" };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    [Test]
    public void Should_Not_Have_Error_When_Barcode_Is_Provided()
    {
        var request = new UpdateIngredientRequest { Barcode = "123" };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    [Test]
    public void Should_Not_Have_Error_When_ImageUrl_Is_Provided()
    {
        var request = new UpdateIngredientRequest { ImageUrl = "https://example.com/image.jpg" };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    [Test]
    public void Should_Not_Have_Error_When_Servings_Are_Provided()
    {
        var request = new UpdateIngredientRequest
        {
            Servings =
            [
                new UpdateIngredientRequest.UpdateIngredientServingRequest
                {
                    Calories = 100,
                    Carbs = 10,
                    Protein = 5,
                    Fat = 2,
                }
            ]
        };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    [Test]
    public void Should_Have_Error_When_Name_Is_Empty()
    {
        var request = new UpdateIngredientRequest { Name = "" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Should_Have_Error_When_Servings_Is_Empty()
    {
        var request = new UpdateIngredientRequest
        {
            Name = "Milk",
            Servings = [],
        };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Servings);
    }
}