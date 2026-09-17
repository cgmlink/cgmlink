using CgmLink.Api.Services;
using CgmLink.Resources;
using FluentValidation;
using System;
using System.Collections.Generic;

namespace CgmLink.Api.Endpoints.Treatments.NewTreatment;

public sealed record NewTreatmentRequest
{
    public DateTimeOffset? Created { get; init; }
    public NewInjectionRequest? Injection { get; init; }
    public Guid? ReadingId { get; init; }
    public ICollection<NewTreatmentMealRequest> Meals { get; init; } = [];
    public ICollection<NewTreatmentIngredientRequest> Ingredients { get; init; } = [];

    public sealed record NewInjectionRequest
    {
        public required Guid InsulinId { get; init; }
        public required decimal Units { get; init; }
    }

    public sealed record NewTreatmentMealRequest : ITreatmentMealRequest
    {
        public required Guid MealId { get; init; }
        public required decimal Quantity { get; init; }
    }

    public sealed record NewTreatmentIngredientRequest : IMealIngredientRequest
    {
        public required Guid IngredientId { get; init; }
        public required Guid ServingId { get; init; }
        public required decimal Quantity { get; init; }
    }

    public sealed class NewTreatmentRequestValidator : AbstractValidator<NewTreatmentRequest>
    {
        public NewTreatmentRequestValidator()
        {
            RuleFor(x => x)
                .Must(x => x.Meals.Count > 0 || x.Ingredients.Count > 0 || x.Injection is not null)
                .WithMessage(ValidationMessages.MealInjectionIdInjectionIdMustBeProvided);

            RuleFor(x => x.Injection!.InsulinId)
                .NotEmpty()
                .WithMessage(ValidationMessages.InsulinIdInvalid)
                .When(x => x.Injection is not null);

            RuleFor(x => x.Injection!.Units)
                .GreaterThan(0)
                .WithMessage(ValidationMessages.UnitsGreaterThanZero)
                .When(x => x.Injection is not null);

            RuleForEach(x => x.Meals).ChildRules(meal =>
            {
                meal.RuleFor(m => m.MealId)
                    .NotEmpty()
                    .WithMessage(ValidationMessages.MealIdInvalid);
                meal.RuleFor(m => m.Quantity)
                    .GreaterThan(0)
                    .WithMessage(ValidationMessages.QuantityGreaterThanZero);
            });

            RuleForEach(x => x.Ingredients).ChildRules(ingredient =>
            {
                ingredient.RuleFor(i => i.IngredientId)
                    .NotEmpty()
                    .WithMessage(ValidationMessages.IngredientIdInvalid);
                ingredient.RuleFor(i => i.ServingId)
                    .NotEmpty()
                    .WithMessage(ValidationMessages.IngredientIdInvalid);
                ingredient.RuleFor(i => i.Quantity)
                    .GreaterThan(0)
                    .WithMessage(ValidationMessages.QuantityGreaterThanZero);
            });

            RuleFor(x => x.ReadingId)
                .NotEmpty()
                .WithMessage(ValidationMessages.ReadingIdRequiredWhenAllNull)
                .When(x => x.ReadingId is not null);
        }
    }
}