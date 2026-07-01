using System;
using FluentValidation.TestHelper;
using CgmLink.Api.Endpoints.Injections.UpdateInjection;
using NUnit.Framework;

namespace CgmLink.Api.Tests.Validators;

[TestFixture]
public class UpdateInjectionRequestValidatorTests
{
    private UpdateInjectionRequest.UpdateInjectionRequestValidator _validator;

    [SetUp]
    public void SetUp()
    {
        _validator = new UpdateInjectionRequest.UpdateInjectionRequestValidator();
    }

    [Test]
    public void Validate_Returns_Error_When_InjectionId_Is_Empty()
    {
        var request = new UpdateInjectionRequest
        {
            InjectionId = Guid.Empty,
            InsulinId = Guid.NewGuid(),
            Units = 10,
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.InjectionId).WithErrorMessage("INJECTION_ID_INVALID");
    }

    [Test]
    public void Validate_Returns_Error_When_InsulinId_Is_Empty()
    {
        var request = new UpdateInjectionRequest
        {
            InjectionId = Guid.NewGuid(),
            InsulinId = Guid.Empty,
            Units = 10,
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.InsulinId).WithErrorMessage("INSULIN_ID_INVALID");
    }

    [Test]
    public void Validate_Returns_Error_When_Units_Are_Less_Than_Or_Equal_To_Zero()
    {
        var request = new UpdateInjectionRequest
        {
            InjectionId = Guid.NewGuid(),
            InsulinId = Guid.NewGuid(),
            Units = 0,
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Units).WithErrorMessage("UNITS_GREATER_THAN_ZERO");
    }

    [Test]
    public void Validate_Passes_When_All_Fields_Are_Valid()
    {
        var request = new UpdateInjectionRequest
        {
            InjectionId = Guid.NewGuid(),
            InsulinId = Guid.NewGuid(),
            Units = 10,
        };

        var result = _validator.TestValidate(request);

        Assert.That(result.IsValid, Is.True);
    }
}
