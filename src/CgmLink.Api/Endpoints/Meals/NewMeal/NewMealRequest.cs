using FluentValidation;
using CgmLink.Api.Services;
using CgmLink.Resources;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CgmLink.Api.Endpoints.Meals.NewMeal;

public sealed record NewMealRequest
{
    public required string Name { get; init; }
    public string? ImageUrl { get; init; }
    public string? ThumbnailUrl { get; init; }
    public ICollection<NewMealIngredientRequest> Ingredients { get; init; } = [];
    public ICollection<NewMealNutritionIngredientRequest> NutritionIngredients { get; init; } = [];

    public sealed record NewMealNutritionIngredientRequest : IMealNutritionIngredientRequest
    {
        public required string ProductId { get; init; }
        public required string ServingId { get; init; }
        public required decimal Quantity { get; init; }
    }

    public sealed record NewMealIngredientRequest : IMealIngredientRequest
    {
        public required Guid IngredientId { get; init; }
        public required Guid ServingId { get; init; }
        public required decimal Quantity { get; init; }
    }

    public sealed class NewMealRequestValidator : AbstractValidator<NewMealRequest>
    {
        public NewMealRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(ValidationMessages.NameRequired);

            RuleFor(x => x.Ingredients)
                .NotNull()
                .Must((request, ingredients) => ingredients is not null &&
                    (ingredients.Count > 0 || request.NutritionIngredients?.Count > 0))
                .WithMessage(ValidationMessages.IngredientsNotEmpty);

            RuleFor(x => x.NutritionIngredients).NotNull();

            RuleForEach(x => x.NutritionIngredients).ChildRules(ingredient =>
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
            });

            RuleFor(x => x.NutritionIngredients)
                .Must(nutritionIngredients => nutritionIngredients.Select(i => i.ProductId).Distinct().Count() == nutritionIngredients.Count)
                .WithMessage(ValidationMessages.DuplicateIngredientId)
                .When(x => x.NutritionIngredients is not null);

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

            RuleFor(x => x.Ingredients)
                .Must(ingredients => ingredients.Select(i => i.IngredientId).Distinct().Count() == ingredients.Count)
                .WithMessage(ValidationMessages.DuplicateIngredientId)
                .When(x => x.Ingredients is not null);
        }
    }
}
