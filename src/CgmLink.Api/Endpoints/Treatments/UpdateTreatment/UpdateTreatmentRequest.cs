using CgmLink.Api.Endpoints.Injections.UpdateInjection;
using FluentValidation;
using System;
using System.Collections.Generic;

namespace CgmLink.Api.Endpoints.Treatments.UpdateTreatment;

public sealed record UpdateTreatmentRequest
{
    public UpdateTreatmentInjectionRequest? Injection { get; set; }
    public ICollection<UpdateTreatmentMealRequest> Meals { get; set; } = [];
    public ICollection<UpdateTreatmentIngredientRequest> Ingredients { get; set; } = [];
    public Guid? ReadingId { get; set; }

    public sealed class UpdateTreatmentRequestValidator : AbstractValidator<UpdateTreatmentRequest>
    {
        public UpdateTreatmentRequestValidator()
        {
            When(x => x.Injection is null && x.ReadingId is null && x.Meals.Count == 0 && x.Ingredients.Count == 0, () =>
            {
                RuleFor(x => x.Injection)
                    .NotNull()
                    .WithMessage(Resources.ValidationMessages.InjectionIdRequiredWhenAllNull);

                RuleFor(x => x.ReadingId)
                    .NotNull()
                    .WithMessage(Resources.ValidationMessages.ReadingIdRequiredWhenAllNull);

                RuleFor(x => x.Meals)
                    .NotEmpty()
                    .WithMessage(Resources.ValidationMessages.MealRequiredWhenAllNull);

                RuleFor(x => x.Ingredients)
                    .NotEmpty()
                    .WithMessage(Resources.ValidationMessages.IngredientRequiredWhenAllNull);
            });
        }
    }

    public record UpdateTreatmentInjectionRequest
    {
        public Guid? Id { get; set; }
        public required Guid InsulinId { get; set; }
        public required double Units { get; set; }
    }

    public record UpdateTreatmentMealRequest
    {
        public Guid Id { get; set; }
        public decimal Quantity { get; set; }
    }

    public record UpdateTreatmentIngredientRequest
    {
        public Guid Id { get; set; }
        public decimal Quantity { get; set; }
    }
}