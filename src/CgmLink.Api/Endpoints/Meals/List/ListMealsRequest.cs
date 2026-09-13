using CgmLink.Api.Models;
using CgmLink.Data.Entities;
using FluentValidation;
using Microsoft.Extensions.Options;
using System.Linq;

namespace CgmLink.Api.Endpoints.Meals.List;

public sealed record ListMealsRequest : PagedRequest
{
    internal static readonly string[] SortFields =
    [
        nameof(Meal.Created),
        nameof(Meal.Updated),
        nameof(Meal.Name),
        nameof(Meal.Calories), 
        nameof(Meal.Carbs), 
        nameof(Meal.Protein), 
        nameof(Meal.Fat)
    ];

    public sealed class ListMealsValidator : PagedRequestValidator<ListMealsRequest>
    {
        public ListMealsValidator(IOptions<ApiSettings> apiSettings) : base(apiSettings)
        {
            RuleFor(x => x.SortBy)
                .Must(field => field is not null && SortFields.Contains(field))
                .WithMessage(Resources.ValidationMessages.SortByInvalid)
                .When(x => !string.IsNullOrWhiteSpace(x.SortBy));
        }
    }
}