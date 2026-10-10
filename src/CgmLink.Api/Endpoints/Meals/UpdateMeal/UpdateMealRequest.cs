using FluentValidation;
using CgmLink.Api.Services;
using CgmLink.Resources;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CgmLink.Api.Endpoints.Meals.UpdateMeal;

public sealed record UpdateMealRequest
{
    public string? Name { get; init; }
    public string? ImageUrl { get; init; }
    public string? ThumbnailUrl { get; init; }
    public ICollection<UpdateMealIngredientRequest>? Ingredients { get; init; }
    public ICollection<UpdateMealNutritionIngredientRequest>? NutritionIngredients { get; init; }

    public sealed record UpdateMealNutritionIngredientRequest : IMealNutritionIngredientRequest
    {
        public required string ProductId { get; init; }
        public required string ServingId { get; init; }
        public required decimal Quantity { get; init; }
    }

    public sealed record UpdateMealIngredientRequest : IMealIngredientRequest
    {
        public required Guid IngredientId { get; init; }
        public required Guid ServingId { get; init; }
        public required decimal Quantity { get; init; }
    }

    public sealed class UpdateMealRequestValidator : AbstractValidator<UpdateMealRequest>
    {
        public UpdateMealRequestValidator()
        {
            RuleFor(x => x)
                .Must(x => x.Name is not null ||
                    x.ImageUrl is not null ||
                    x.ThumbnailUrl is not null ||
                    x.Ingredients is not null ||
                    x.NutritionIngredients is not null)
                .WithMessage(ValidationMessages.MealRequiredWhenAllNull);

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(ValidationMessages.NameRequired)
                .When(x => x.Name is not null);

            RuleFor(x => x.Ingredients)
                .NotEmpty()
                .WithMessage(ValidationMessages.IngredientsNotEmpty)
                .When(x => x.Ingredients is not null && !(x.NutritionIngredients?.Count > 0));

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
            }).When(x => x.NutritionIngredients is not null);

            RuleFor(x => x.NutritionIngredients)
                .Must(ingredients => ingredients is null ||
                    ingredients.Select(i => i.ProductId).Distinct().Count() == ingredients.Count)
                .WithMessage(ValidationMessages.DuplicateIngredientId);

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
        }
    }
}
