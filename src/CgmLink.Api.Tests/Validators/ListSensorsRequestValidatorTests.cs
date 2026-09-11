using FluentValidation.TestHelper;
using CgmLink.Api.Endpoints.Sensors.List;
using CgmLink.Api.Models;
using CgmLink.Data.Enums;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace CgmLink.Api.Tests.Validators;

[TestFixture]
class ListSensorsRequestValidatorTests
{
    private readonly ListSensorsRequest.ListSensorsValidator _validator;

    public ListSensorsRequestValidatorTests()
    {
        var apiSettings = Options.Create(new ApiSettings
        {
            MaxPageSize = 25
        });
        _validator = new ListSensorsRequest.ListSensorsValidator(apiSettings);
    }

    [Test]
    public void Should_Have_Error_When_Page_Is_Less_Than_Zero()
    {
        var request = new ListSensorsRequest
        {
            Page = -1,
            PageSize = 10
        };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(r => r.Page);
    }

    [Test]
    public void Should_Have_Error_When_PageSize_Is_Less_Than_One()
    {
        var request = new ListSensorsRequest
        {
            Page = 1,
            PageSize = 0
        };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(r => r.PageSize);
    }

    [Test]
    public void Should_Have_Error_When_PageSize_Exceeds_MaxPageSize()
    {
        var request = new ListSensorsRequest
        {
            Page = 1,
            PageSize = 26
        };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(r => r.PageSize);
    }

    [Test]
    public void Should_Not_Have_Error_For_Valid_Request()
    {
        var request = new ListSensorsRequest
        {
            Page = 1,
            PageSize = 10
        };

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Should_Have_Error_When_SortDirection_Is_Invalid()
    {
        var request = new ListSensorsRequest
        {
            Page = 1,
            PageSize = 10,
            SortDirection = (SortDirection)(-1)
        };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(r => r.SortDirection);
    }

    [Test]
    public void Should_Have_Error_When_SortBy_Is_Not_Allowed()
    {
        var request = new ListSensorsRequest
        {
            Page = 1,
            PageSize = 10,
            SortBy = "Bogus"
        };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(r => r.SortBy);
    }

    [Test]
    public void Should_Not_Have_Error_When_SortBy_Is_Allowed()
    {
        var request = new ListSensorsRequest
        {
            Page = 1,
            PageSize = 10,
            SortBy = "Started",
            SortDirection = SortDirection.Asc
        };

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Should_Not_Have_Error_When_SortBy_Is_Blank()
    {
        var request = new ListSensorsRequest
        {
            Page = 1,
            PageSize = 10,
            SortBy = ""
        };

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }
}