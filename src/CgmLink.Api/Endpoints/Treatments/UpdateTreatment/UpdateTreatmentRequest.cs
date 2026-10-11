using CgmLink.Api.Services;
using CgmLink.Resources;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CgmLink.Api.Endpoints.Treatments.UpdateTreatment;

public sealed record UpdateTreatmentRequest
{
    public UpdateInjectionRequest? Injection { get; init; }
    public Guid? ReadingId { get; init; }
    public ICollection<UpdateTreatmentMealRequest>? Meals { get; init; }
    public ICollection<UpdateTreatmentIngredientRequest>? Ingredients { get; init; }
    public ICollection<UpdateTreatmentNutritionIngredientRequest>? NutritionIngredients { get; init; }

    public sealed record UpdateTreatmentNutritionIngredientRequest : IMealNutritionIngredientRequest
    {
        public required string ProductId { get; init; }
        public required string ServingId { get; init; }
        public required decimal Quantity { get; init; }
    }

    public sealed record UpdateInjectionRequest
    {
        public required Guid InsulinId { get; init; }
        public required decimal Units { get; init; }
    }

    public sealed record UpdateTreatmentMealRequest : ITreatmentMealRequest
    {
        public required Guid MealId { get; init; }
        public required decimal Quantity { get; init; }
    }

    public sealed record UpdateTreatmentIngredientRequest : IMealIngredientRequest
    {
        public required Guid IngredientId { get; init; }
        public required Guid ServingId { get; init; }
        public required decimal Quantity { get; init; }
    }

    public sealed class UpdateTreatmentRequestValidator : AbstractValidator<UpdateTreatmentRequest>
    {
        public UpdateTreatmentRequestValidator()
        {
            RuleFor(x => x)
                .Must(x => x.Injection is not null ||
                    x.ReadingId is not null ||
                    x.Meals is not null ||
                    x.Ingredients is not null ||
                    x.NutritionIngredients is not null)
                .WithMessage(ValidationMessages.TreatmentRequiredWhenAllNull);

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
            }).When(x => x.Meals is not null);

            RuleFor(x => x.Meals)
                .Must(meals => meals is null ||
                    meals.Select(m => m.MealId).Distinct().Count() == meals.Count)
                .WithMessage(ValidationMessages.DuplicateMealId)
                .When(x => x.Meals is not null);

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
            }).When(x => x.Ingredients is not null);

            RuleFor(x => x.Ingredients)
                .Must(ingredients => ingredients is null ||
                    ingredients.Select(i => i.IngredientId).Distinct().Count() == ingredients.Count)
                .WithMessage(ValidationMessages.DuplicateIngredientId)
                .When(x => x.Ingredients is not null);

            RuleForEach(x => x.NutritionIngredients).NotNull().ChildRules(ingredient =>
            {
                ingredient.RuleFor(i => i.ProductId)
                    .NotEmpty()
                    .WithMessage(ValidationMessages.IngredientIdInvalid);
                ingredient.RuleFor(i => i.ServingId)
                    .NotEmpty()
                    .WithMessage(ValidationMessages.IngredientServingIdInvalid);
                ingredient.RuleFor(i => i.Quantity)
                    .GreaterThan(0)
                    .WithMessage(ValidationMessages.QuantityGreaterThanZero);
            }).When(x => x.NutritionIngredients is not null);

            RuleFor(x => x.NutritionIngredients)
                .Must(ingredients => ingredients is null ||
                    ingredients.Select(i => i.ProductId).Distinct().Count() == ingredients.Count)
                .WithMessage(ValidationMessages.DuplicateIngredientId)
                .When(x => x.NutritionIngredients?.All(i => i is not null) ?? false);
        }
    }
}
