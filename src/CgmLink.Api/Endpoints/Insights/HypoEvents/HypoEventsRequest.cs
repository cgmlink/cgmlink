using FluentValidation;
using System;

namespace CgmLink.Api.Endpoints.Insights.HypoEvents;

public sealed class HypoEventsRequest
{
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }

    public sealed class Validator : AbstractValidator<HypoEventsRequest>
    {
        public Validator()
        {
            RuleFor(x => x.From).LessThan(x => x.To).WithMessage(Resources.ValidationMessages.ToBeforeFrom);
        }
    }
}
