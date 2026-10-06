using CgmLink.Api.Models;
using CgmLink.Data.Entities;
using FluentValidation;
using Microsoft.Extensions.Options;
using System.Linq;

namespace CgmLink.Api.Endpoints.Ingredients.ListIngredients;

public sealed record ListIngredientsRequest : PagedRequest
{
    public IngredientType? Type { get; init; }
    public string? Name { get; init; }

    internal static readonly string[] SortFields =
        [nameof(Ingredient.Created), nameof(Ingredient.Name), nameof(Ingredient.Updated)];

    public sealed class ListIngredientsValidator : PagedRequestValidator<ListIngredientsRequest>
    {
        public ListIngredientsValidator(IOptions<ApiSettings> apiSettings) : base(apiSettings)
        {
            RuleFor(x => x.Type).IsInEnum().When(x => x.Type is not null);
            RuleFor(x => x.Name).NotEmpty().When(x => x.Type == IngredientType.External || x.Name is not null);
            RuleFor(x => x.SortBy).Empty().When(x => x.Type == IngredientType.External);
            RuleFor(x => x.SortDirection).Null().When(x => x.Type == IngredientType.External);
            RuleFor(x => x)
                .Must(x => (long)x.Page * x.PageSize <= int.MaxValue)
                .WithMessage("The requested page is too large.")
                .When(x => x.Page >= 0 && x.PageSize > 0);
            RuleFor(x => x.SortBy)
                .Must(field => field is not null && ListIngredientsRequest.SortFields.Contains(field))
                .WithMessage(Resources.ValidationMessages.SortByInvalid)
                .When(x => !string.IsNullOrWhiteSpace(x.SortBy));
        }
    }
}
