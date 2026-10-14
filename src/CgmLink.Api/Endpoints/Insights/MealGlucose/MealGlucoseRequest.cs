using System;
using FluentValidation;

namespace CgmLink.Api.Endpoints.Insights.MealGlucose;

public sealed record MealGlucoseRequest
{
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public int? IntervalMinutes { get; init; }

    public sealed class Validator : AbstractValidator<MealGlucoseRequest>
    {
        public Validator()
        {
            RuleFor(x => x.From).LessThan(x => x.To).WithMessage(Resources.ValidationMessages.ToBeforeFrom);
            RuleFor(x => x.IntervalMinutes)
                .GreaterThanOrEqualTo(MealGlucoseConstants.MinimumIntervalMinutes)
                .WithMessage(Resources.ValidationMessages.IntervalInvalid)
                .Must((request, interval) => !request.From.HasValue || !request.To.HasValue ||
                    (interval ?? MealGlucoseConstants.DefaultIntervalMinutes) <= (request.To.Value - request.From.Value).TotalMinutes)
                .WithMessage(Resources.ValidationMessages.IntervalInvalid);
            RuleFor(x => x.From).GreaterThanOrEqualTo(DateTimeOffset.MinValue.AddMinutes(MealGlucoseConstants.BaselineMinutes));
            RuleFor(x => x.To).LessThanOrEqualTo(DateTimeOffset.MaxValue.AddMinutes(
                -MealGlucoseConstants.WindowMinutes - MealGlucoseConstants.ToleranceMinutes));
        }
    }
}
