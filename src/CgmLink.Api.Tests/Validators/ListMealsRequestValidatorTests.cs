using FluentValidation.TestHelper;
using CgmLink.Api.Endpoints.Meals.ListMeals;
using CgmLink.Api.Models;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace CgmLink.Api.Tests.Validators;

[TestFixture]
class ListMealsRequestValidatorTests
{
    private readonly ListMealsRequest.ListMealsValidator _validator;

    public ListMealsRequestValidatorTests()
    {
        var apiSettings = Options.Create(new ApiSettings
        {
            MaxPageSize = 25
        });
        _validator = new ListMealsRequest.ListMealsValidator(apiSettings);
    }

    [Test]
    public void Should_Have_Error_When_Page_Is_Less_Than_Zero()
    {
        var request = new ListMealsRequest { Page = -1, PageSize = 10 };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(r => r.Page);
    }

    [Test]
    public void Should_Have_Error_When_PageSize_Is_Less_Than_One()
    {
        var request = new ListMealsRequest { Page = 0, PageSize = 0 };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(r => r.PageSize);
    }

    [Test]
    public void Should_Have_Error_When_PageSize_Exceeds_MaxPageSize()
    {
        var request = new ListMealsRequest { Page = 0, PageSize = 26 };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(r => r.PageSize);
    }

    [Test]
    public void Should_Not_Have_Error_For_Valid_Request()
    {
        var request = new ListMealsRequest { Page = 0, PageSize = 10 };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Should_Not_Have_Error_When_SortBy_Is_Supported()
    {
        var request = new ListMealsRequest { Page = 0, PageSize = 10, SortBy = "Name" };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(r => r.SortBy);
    }

    [Test]
    public void Should_Not_Have_Error_When_SortBy_Is_Not_Provided()
    {
        var request = new ListMealsRequest { Page = 0, PageSize = 10 };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(r => r.SortBy);
    }

    [Test]
    public void Should_Have_Error_When_SortBy_Is_Not_Supported()
    {
        var request = new ListMealsRequest { Page = 0, PageSize = 10, SortBy = "Bogus" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(r => r.SortBy);
    }

    [Test]
    public void Should_Have_Error_When_SortDirection_Is_Invalid()
    {
        var request = new ListMealsRequest { Page = 0, PageSize = 10, SortDirection = (SortDirection)(-1) };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(r => r.SortDirection);
    }
}