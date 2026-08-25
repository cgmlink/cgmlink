using System.Collections.Generic;
using FluentValidation;
using CgmLink.Api.Models;

namespace CgmLink.Api.Endpoints.Ingredients.UpdateIngredient;

public sealed record UpdateIngredientRequest
{
    public string? Barcode { get; set; }
    public IReadOnlyList<string>? Brands { get; set; }
    public required string Name { get; set; }
    public required decimal Carbs { get; set; }
    public required decimal Protein { get; set; }
    public required decimal Fat { get; set; }
    public required decimal Calories { get; set; }
    public required UnitOfMeasurement Uom { get; set; }

    public sealed class UpdateIngredientRequestValidator : AbstractValidator<UpdateIngredientRequest>
    {
        public UpdateIngredientRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(Resources.ValidationMessages.NameRequired);
            RuleFor(x => x.Carbs)
                .GreaterThanOrEqualTo(0)
                .WithMessage(Resources.ValidationMessages.CarbsGreaterThanOrEqualToZero);
            RuleFor(x => x.Protein)
                .GreaterThanOrEqualTo(0)
                .WithMessage(Resources.ValidationMessages.ProteinGreaterThanOrEqualToZero);
            RuleFor(x => x.Fat)
                .GreaterThanOrEqualTo(0)
                .WithMessage(Resources.ValidationMessages.FatGreaterThanOrEqualToZero);
            RuleFor(x => x.Calories)
                .GreaterThanOrEqualTo(0)
                .WithMessage(Resources.ValidationMessages.CaloriesGreaterThanOrEqualToZero);
            RuleFor(x => x.Uom)
                .Must(uom => uom is UnitOfMeasurement.Unit or UnitOfMeasurement.Hectograms)
                .When(x => !string.IsNullOrWhiteSpace(x.Barcode))
                .WithMessage(Resources.ValidationMessages.UomInvalidForNutritionProduct);
        }
    }
}
