using System;
using FluentValidation;

namespace CgmLink.Api.Endpoints.Insights.TotalNutrition;

public sealed record TotalNutritionRequest
{
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }

    public sealed class Validator : AbstractValidator<TotalNutritionRequest>
    {
        public Validator()
        {
            RuleFor(x => x.From).LessThan(x => x.To).WithMessage(Resources.ValidationMessages.ToBeforeFrom);
        }
    }
}
