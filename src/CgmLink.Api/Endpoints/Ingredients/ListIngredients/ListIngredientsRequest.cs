using CgmLink.Api.Models;
using CgmLink.Data.Entities;
using FluentValidation;
using Microsoft.Extensions.Options;
using System.Linq;

namespace CgmLink.Api.Endpoints.Ingredients.ListIngredients;

public sealed record ListIngredientsRequest : PagedRequest
{
    internal static readonly string[] SortFields =
        [nameof(Ingredient.Created), nameof(Ingredient.Name), nameof(Ingredient.Updated)];

    public sealed class ListIngredientsValidator : PagedRequestValidator<ListIngredientsRequest>
    {
        public ListIngredientsValidator(IOptions<ApiSettings> apiSettings) : base(apiSettings)
        {
            RuleFor(x => x.SortBy)
                .Must(field => field is not null && ListIngredientsRequest.SortFields.Contains(field))
                .WithMessage(Resources.ValidationMessages.SortByInvalid)
                .When(x => !string.IsNullOrWhiteSpace(x.SortBy));
        }
    }
}