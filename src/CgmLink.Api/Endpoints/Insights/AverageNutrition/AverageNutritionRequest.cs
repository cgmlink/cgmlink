using System;
using FluentValidation;

namespace CgmLink.Api.Endpoints.Insights.AverageNutrition;

public sealed record AverageNutritionRequest
{
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }

    public sealed class Validator : AbstractValidator<AverageNutritionRequest>
    {
        public Validator()
        {
            RuleFor(x => x.From).LessThan(x => x.To).WithMessage(Resources.ValidationMessages.ToBeforeFrom);
        }
    }
}
