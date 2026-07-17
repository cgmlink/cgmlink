using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using CgmLink.Api.Models;
using Microsoft.Extensions.Options;

namespace CgmLink.Api.Endpoints.Food.List;

public sealed record ListFoodRequest : PagedRequest
{
    public string? Search { get; set; }

    public string? Sort { get; set; }

    public SortDirection? SortDirection { get; set; }

    public bool IsDescending() => SortDirection == Models.SortDirection.Desc || SortDirection == null;

    public List<FoodSortOrder> GetSortValues()
    {
        if (string.IsNullOrWhiteSpace(Sort))
        {
            return [FoodSortOrder.Created];
        }

        var values = new List<FoodSortOrder>();
        foreach (var part in Sort.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (Enum.TryParse<FoodSortOrder>(part.Trim(), true, out var parsed))
            {
                values.Add(parsed);
            }
        }

        return values.Count > 0 ? values : [FoodSortOrder.Created];
    }

    public sealed class ListFoodValidator : PagedRequestValidator<ListFoodRequest>
    {
        private static readonly HashSet<string> ValidSortValues = new(StringComparer.OrdinalIgnoreCase)
        {
            "Created",
            "Updated",
            "Name",
            "TotalCalories",
            "TotalCarbs",
            "TotalProtein",
            "TotalFat"
        };

        public ListFoodValidator(IOptions<ApiSettings> apiSettings) : base(apiSettings)
        {
            RuleFor(x => x.Search)
                .MinimumLength(3)
                .When(x => !string.IsNullOrEmpty(x.Search))
                .WithMessage(Resources.ValidationMessages.SearchLengthInvalid);

            RuleFor(x => x.Sort)
                .Must(x =>
                {
                    if (string.IsNullOrWhiteSpace(x)) return true;
                    var parts = x.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    return parts.All(p => ValidSortValues.Contains(p.Trim()));
                })
                .When(x => !string.IsNullOrEmpty(x.Sort))
                .WithMessage("Sort must be a comma-separated list of: Created, Updated, Name, TotalCalories, TotalCarbs, TotalProtein, TotalFat");

            RuleFor(x => x.SortDirection)
                .NotNull()
                .When(x => !string.IsNullOrEmpty(x.Sort))
                .WithMessage("SortDirection is required when Sort is provided");
        }
    }
}
