using FluentValidation.TestHelper;
using CgmLink.Api.Endpoints.Ingredients.ListIngredients;
using CgmLink.Api.Endpoints.Ingredients;
using CgmLink.Api.Models;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace CgmLink.Api.Tests.Validators;

[TestFixture]
class ListIngredientsRequestValidatorTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void ExternalSearch_RequiresName(string name)
    {
        _validator.TestValidate(new ListIngredientsRequest { Type = IngredientType.External, Name = name, PageSize = 20 })
            .ShouldHaveValidationErrorFor(request => request.Name);
    }

    [Test]
    public void PersonalList_IsValid_When_PageSize_Is_Provided()
    {
        var request = new ListIngredientsRequest { PageSize = 20 };
        Assert.That(request.Type, Is.Null);
        Assert.That(_validator.Validate(request).IsValid, Is.True);
    }

    [Test]
    public void Should_Have_Error_When_PageSize_Is_Not_Provided()
    {
        _validator.TestValidate(new ListIngredientsRequest())
            .ShouldHaveValidationErrorFor(request => request.PageSize);
    }

    [TestCase((IngredientType)99)]
    public void InvalidType_IsRejected(IngredientType type)
    {
        _validator.TestValidate(new ListIngredientsRequest { Type = type, PageSize = 20 })
            .ShouldHaveValidationErrorFor(request => request.Type);
    }

    [Test]
    public void ExternalSearch_RejectsLocalSorting()
    {
        var result = _validator.TestValidate(new ListIngredientsRequest
        {
            Type = IngredientType.External, Name = "Milk", PageSize = 20, SortBy = "Name", SortDirection = SortDirection.Asc,
        });
        result.ShouldHaveValidationErrorFor(request => request.SortBy);
        result.ShouldHaveValidationErrorFor(request => request.SortDirection);
    }

    [Test]
    public void PaginationOverflow_IsRejected()
    {
        Assert.That(_validator.Validate(new ListIngredientsRequest { Page = int.MaxValue, PageSize = 20 }).IsValid, Is.False);
    }

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
