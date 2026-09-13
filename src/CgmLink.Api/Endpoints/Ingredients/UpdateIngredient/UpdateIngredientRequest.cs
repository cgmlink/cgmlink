using FluentValidation;
using CgmLink.Resources;
using System.Collections.Generic;

namespace CgmLink.Api.Endpoints.Ingredients.UpdateIngredient;

public sealed record UpdateIngredientRequest
{
    public string? Name { get; init; }
    public string? Barcode { get; init; }
    public string? ImageUrl { get; init; }
    public string? ThumbnailUrl { get; init; }
    public ICollection<UpdateIngredientServingRequest>? Servings { get; init; }

    public sealed record UpdateIngredientServingRequest
    {
        public string? Description { get; init; }
        public decimal? ServingAmount { get; init; }
        public string? ServingUnit { get; init; }
        public required decimal Calories { get; init; }
        public required decimal Carbs { get; init; }
        public required decimal Protein { get; init; }
        public required decimal Fat { get; init; }
    }

    public sealed class UpdateIngredientRequestValidator : AbstractValidator<UpdateIngredientRequest>
    {
        public UpdateIngredientRequestValidator()
        {
            RuleFor(x => x)
                .Must(x => x.Name is not null ||
                    x.Barcode is not null ||
                    x.ImageUrl is not null ||
                    x.ThumbnailUrl is not null ||
                    x.Servings is not null)
                .WithMessage(ValidationMessages.IngredientRequiredWhenAllNull);

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(ValidationMessages.NameRequired)
                .When(x => x.Name is not null);

            RuleFor(x => x.Servings)
                .NotEmpty()
                .WithMessage(ValidationMessages.IngredientsNotEmpty)
                .When(x => x.Servings is not null);

            RuleForEach(x => x.Servings)
                .ChildRules(serving =>
                {
                    serving.RuleFor(s => s.Calories)
                        .GreaterThanOrEqualTo(0)
                        .WithMessage(ValidationMessages.CaloriesGreaterThanOrEqualToZero);
                    serving.RuleFor(s => s.Carbs)
                        .GreaterThanOrEqualTo(0)
                        .WithMessage(ValidationMessages.CarbsGreaterThanOrEqualToZero);
                    serving.RuleFor(s => s.Protein)
                        .GreaterThanOrEqualTo(0)
                        .WithMessage(ValidationMessages.ProteinGreaterThanOrEqualToZero);
                    serving.RuleFor(s => s.Fat)
                        .GreaterThanOrEqualTo(0)
                        .WithMessage(ValidationMessages.FatGreaterThanOrEqualToZero);
                })
                .When(x => x.Servings is not null);
        }
    }
}