using System;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace CgmLink.Api.Endpoints.Insights.MealGlucose;

public sealed record MealGlucoseRequest
{
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public int? IntervalMinutes { get; init; }

    public sealed class Validator : AbstractValidator<MealGlucoseRequest>
    {
        public Validator(IOptions<MealGlucoseSettings> mealGlucoseSettings)
        {
            var settings = mealGlucoseSettings.Value;
            RuleFor(x => x.From).LessThan(x => x.To).WithMessage(Resources.ValidationMessages.ToBeforeFrom);
            RuleFor(x => x.IntervalMinutes)
                .GreaterThanOrEqualTo(settings.MinimumIntervalMinutes)
                .WithMessage(Resources.ValidationMessages.IntervalInvalid)
                .Must((request, interval) => !request.From.HasValue || !request.To.HasValue ||
                    (interval ?? settings.DefaultIntervalMinutes) <= (request.To.Value - request.From.Value).TotalMinutes)
                .WithMessage(Resources.ValidationMessages.IntervalInvalid);
            RuleFor(x => x.From).GreaterThanOrEqualTo(DateTimeOffset.MinValue.AddMinutes(settings.BaselineMinutes));
            RuleFor(x => x.To).LessThanOrEqualTo(DateTimeOffset.MaxValue.AddMinutes(
                -(double)settings.WindowMinutes - settings.ToleranceMinutes));
        }
    }
}
