using CgmLink.Api.Models;
using CgmLink.Data.Entities;
using FluentValidation;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace CgmLink.Api.Endpoints.Pens.ListPens;

public sealed record ListPensRequest : PagedRequest
{
    internal static readonly IReadOnlyDictionary<string, Expression<Func<Pen, object>>> SortFields =
        new Dictionary<string, Expression<Func<Pen, object>>>
        {
            ["Created"] = p => p.Created,
            ["Updated"] = p => p.Updated ?? p.Created,
            ["StartTime"] = p => p.StartTime,
        };

    public sealed class ListPensValidator : PagedRequestValidator<ListPensRequest>
    {
        public ListPensValidator(IOptions<ApiSettings> apiSettings) : base(apiSettings)
        {
            RuleFor(x => x.SortBy)
                .Must(field => field is not null && ListPensRequest.SortFields.ContainsKey(field))
                .WithMessage(Resources.ValidationMessages.SortByInvalid)
                .When(x => !string.IsNullOrWhiteSpace(x.SortBy));
        }
    }
}