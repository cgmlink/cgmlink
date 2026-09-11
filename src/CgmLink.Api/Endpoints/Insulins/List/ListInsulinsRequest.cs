using FluentValidation;
using CgmLink.Api.Models;
using CgmLink.Data.Entities;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace CgmLink.Api.Endpoints.Insulins.List;

public sealed record ListInsulinsRequest : PagedRequest
{
    public InsulinType? Type { get; init; }

    internal static readonly IReadOnlyDictionary<string, Expression<Func<Insulin, object>>> SortFields =
        new Dictionary<string, Expression<Func<Insulin, object>>>
        {
            ["Created"] = i => i.Created,
            ["Updated"] = i => i.Updated ?? i.Created,
            ["Name"] = i => i.Name,
            ["Type"] = i => i.Type,
        };

    public sealed class ListInsulinsValidator : PagedRequestValidator<ListInsulinsRequest>
    {
        public ListInsulinsValidator(IOptions<ApiSettings> apiSettings) : base(apiSettings)
        {
            RuleFor(x => x.Type).IsInEnum().When(x => x.Type is not null).WithMessage(Resources.ValidationMessages.InsulinTypeInvalid);
            RuleFor(x => x.SortBy)
                .Must(field => field is not null && ListInsulinsRequest.SortFields.ContainsKey(field))
                .WithMessage(Resources.ValidationMessages.SortByInvalid)
                .When(x => !string.IsNullOrWhiteSpace(x.SortBy));
        }
    }
}