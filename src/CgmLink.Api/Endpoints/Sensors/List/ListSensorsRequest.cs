using CgmLink.Api.Models;
using CgmLink.Data.Entities;
using FluentValidation;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace CgmLink.Api.Endpoints.Sensors.List;

public sealed record ListSensorsRequest : PagedRequest
{
    internal static readonly IReadOnlyDictionary<string, Expression<Func<Sensor, object>>> SortFields =
        new Dictionary<string, Expression<Func<Sensor, object>>>
        {
            ["Created"] = s => s.Created,
            ["Started"] = s => s.Started,
            ["Expires"] = s => s.Expires,
        };

    public sealed class ListSensorsValidator : PagedRequestValidator<ListSensorsRequest>
    {
        public ListSensorsValidator(IOptions<ApiSettings> apiSettings) : base(apiSettings)
        {
            RuleFor(x => x.SortBy)
                .Must(field => field is not null && ListSensorsRequest.SortFields.ContainsKey(field))
                .WithMessage(Resources.ValidationMessages.SortByInvalid)
                .When(x => !string.IsNullOrWhiteSpace(x.SortBy));
        }
    }
}