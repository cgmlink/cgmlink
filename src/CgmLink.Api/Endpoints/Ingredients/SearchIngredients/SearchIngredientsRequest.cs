using FluentValidation;
using CgmLink.Api.Models;
using Microsoft.Extensions.Options;

namespace CgmLink.Api.Endpoints.Ingredients.SearchIngredients;

public sealed record SearchIngredientsRequest
{
    public required string Name { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; } = 20;
    public bool IncludeExternal { get; init; } = true;

    public sealed class Validator : AbstractValidator<SearchIngredientsRequest>
    {
        public Validator(IOptions<ApiSettings> apiSettings)
        {
            RuleFor(request => request.Name).NotEmpty();
            RuleFor(request => request.Page).GreaterThanOrEqualTo(0);
            RuleFor(request => request.PageSize).InclusiveBetween(1, apiSettings.Value.MaxPageSize);
            RuleFor(request => request)
                .Must(request => (long)request.Page * request.PageSize <= int.MaxValue)
                .WithMessage("The requested page is too large.")
                .When(request => request.Page >= 0 && request.PageSize > 0);
        }
    }
}
