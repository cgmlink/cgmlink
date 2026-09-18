using FluentValidation.TestHelper;
using CgmLink.Api.Endpoints.Ingredients.ListIngredients;
using CgmLink.Api.Models;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace CgmLink.Api.Tests.Validators;

[TestFixture]
class ListIngredientsRequestValidatorTests
{
    private readonly ListIngredientsRequest.ListIngredientsValidator _validator;

    public ListIngredientsRequestValidatorTests()
    {
        var apiSettings = Options.Create(new ApiSettings
        {
            MaxPageSize = 25
        });
        _validator = new ListIngredientsRequest.ListIngredientsValidator(apiSettings);
    }

    [Test]
    public void Should_Not_Have_Error_When_SortBy_Is_Supported()
    {
        var request = new ListIngredientsRequest { Page = 0, PageSize = 10, SortBy = "Name" };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(r => r.SortBy);
    }

    [Test]
    public void Should_Not_Have_Error_When_SortBy_Is_Not_Provided()
    {
        var request = new ListIngredientsRequest { Page = 0, PageSize = 10 };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(r => r.SortBy);
    }

    [Test]
    public void Should_Have_Error_When_SortBy_Is_Not_Supported()
    {
        var request = new ListIngredientsRequest { Page = 0, PageSize = 10, SortBy = "Bogus" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(r => r.SortBy);
    }

    [Test]
    public void Should_Have_Error_When_SortDirection_Is_Invalid()
    {
        var request = new ListIngredientsRequest { Page = 0, PageSize = 10, SortDirection = (SortDirection)(-1) };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(r => r.SortDirection);
    }
}