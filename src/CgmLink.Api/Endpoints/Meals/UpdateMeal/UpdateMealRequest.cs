using FluentValidation;
using System.Collections.Generic;
using System;

namespace CgmLink.Api.Endpoints.Meals.UpdateMeal;

public sealed record UpdateMealRequest
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public ICollection<UpdateMealIngredientRequest> MealIngredients { get; set; } = [];

    public sealed class UpdateMealRequestValidator : AbstractValidator<UpdateMealRequest>
    {
        public UpdateMealRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(Resources.ValidationMessages.NameRequired);
            RuleForEach(x => x.MealIngredients).SetValidator(new UpdateMealIngredientRequest.UpdateMealIngredientValidator());
        }
    }
}

public sealed record UpdateMealIngredientRequest
{
    public required Guid IngredientId { get; set; }
    public required decimal Quantity { get; set; }

    public sealed class UpdateMealIngredientValidator : AbstractValidator<UpdateMealIngredientRequest>
    {
        public UpdateMealIngredientValidator()
        {
            RuleFor(x => x.IngredientId).NotEmpty().WithMessage(Resources.ValidationMessages.IngredientIdInvalid);
            RuleFor(x => x.Quantity).GreaterThan(0).WithMessage(Resources.ValidationMessages.QuantityGreaterThanZero);
        }
    }
}
