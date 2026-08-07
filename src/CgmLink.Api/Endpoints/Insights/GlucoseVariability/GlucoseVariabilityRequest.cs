using FluentValidation;
using System;

namespace CgmLink.Api.Endpoints.Insights.GlucoseVariability;

public sealed class GlucoseVariabilityRequest
{
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }

    public sealed class Validator : AbstractValidator<GlucoseVariabilityRequest>
    {
        public Validator()
        {
            RuleFor(x => x.From).LessThan(x => x.To).WithMessage(Resources.ValidationMessages.ToBeforeFrom);
        }
    }
}
