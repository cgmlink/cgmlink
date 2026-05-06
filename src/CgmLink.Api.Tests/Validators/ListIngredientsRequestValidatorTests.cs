using FluentValidation.TestHelper;
using CgmLink.Api.Endpoints.Ingredients.List;
using CgmLink.Api.Endpoints.Meals.List;
using CgmLink.Api.Models;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace CgmLink.Api.Tests.Validators;

[TestFixture]
public class ListIngredientsRequestValidatorTests
{
    private readonly ListIngredientsRequest.ListIngredientsValidator _validator;

    public ListIngredientsRequestValidatorTests()
    {
        var apiSettings = Options.Create(new ApiSettings { MaxPageSize = 100 });
        _validator = new ListIngredientsRequest.ListIngredientsValidator(apiSettings);
    }

    [Test]
    public void Should_Have_Error_When_Search_Is_Less_Than_3_Characters()
    {
        var model = new ListIngredientsRequest { Search = "ab" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Search)
            .WithErrorMessage("SEARCH_LENGTH_INVALID");
    }

    [Test]
    public void Should_Not_Have_Error_When_Search_Is_3_Characters_Or_More()
    {
        var model = new ListIngredientsRequest { Search = "abc" };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Search);
    }

    [Test]
    public void Should_Not_Have_Error_When_Search_Is_Null()
    {
        var model = new ListIngredientsRequest { Search = null };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Search);
    }

    [Test]
    public void Should_Not_Have_Error_When_Search_Is_Empty()
    {
        var model = new ListIngredientsRequest { Search = string.Empty };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Search);
    }
}
