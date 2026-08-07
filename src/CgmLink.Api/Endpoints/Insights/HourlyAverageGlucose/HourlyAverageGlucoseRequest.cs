using System;
using FluentValidation;

namespace CgmLink.Api.Endpoints.Insights.HourlyAverageGlucose;

public sealed class HourlyAverageGlucoseRequest
{
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }

    public sealed class Validator : AbstractValidator<HourlyAverageGlucoseRequest>
    {
        public Validator()
        {
            RuleFor(x => x.From).LessThan(x => x.To).WithMessage(Resources.ValidationMessages.ToBeforeFrom);
        }
    }
}
