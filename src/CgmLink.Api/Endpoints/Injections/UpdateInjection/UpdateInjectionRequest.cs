using FluentValidation;
using CgmLink.Api.Models;
using System;

namespace CgmLink.Api.Endpoints.Injections.UpdateInjection;

public record UpdateInjectionRequest
{
    public required Guid InjectionId { get; init; }
    public required Guid InsulinId { get; init; }
    public required double Units { get; init; }

    public sealed class UpdateInjectionRequestValidator : AbstractValidator<UpdateInjectionRequest>
    {
        public UpdateInjectionRequestValidator()
        {
            RuleFor(x => x.InjectionId)
                .NotEmpty()
                .WithMessage(Resources.ValidationMessages.InjectionIdInvalid);
            RuleFor(x => x.InsulinId)
                .NotEmpty()
                .WithMessage(Resources.ValidationMessages.InsulinIdInvalid);
            RuleFor(x => x.Units)
                .GreaterThan(0)
                .WithMessage(Resources.ValidationMessages.UnitsGreaterThanZero);
        }
    }
}