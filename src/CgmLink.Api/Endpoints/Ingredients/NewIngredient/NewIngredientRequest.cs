using FluentValidation;
using CgmLink.Resources;
using System.Collections.Generic;

namespace CgmLink.Api.Endpoints.Ingredients.NewIngredient;

public sealed record NewIngredientRequest
{
    public required string Name { get; init; }
    public string? Barcode { get; init; }
    public string? ProductId { get; init; }
    public string? ImageUrl { get; init; }
    public string? ThumbnailUrl { get; init; }
    public ICollection<NewIngredientServingRequest> Servings { get; init; } = [];

    public sealed record NewIngredientServingRequest
    {
        public string? Description { get; init; }
        public decimal? ServingAmount { get; init; }
        public string? ServingUnit { get; init; }
        public required decimal Calories { get; init; }
        public required decimal Carbs { get; init; }
        public required decimal Protein { get; init; }
        public required decimal Fat { get; init; }
    }

    public sealed class NewIngredientRequestValidator : AbstractValidator<NewIngredientRequest>
    {
        public NewIngredientRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(ValidationMessages.NameRequired);

            RuleFor(x => x.Servings)
                .NotEmpty();

            RuleForEach(x => x.Servings).ChildRules(serving =>
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
            });
        }
    }
}
