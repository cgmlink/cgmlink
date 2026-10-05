using FluentValidation;

namespace CgmLink.Api.Endpoints.Ingredients.SearchIngredients;

public sealed record SearchIngredientsRequest
{
    public required string Name { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; } = 20;
    public bool IncludeExternal { get; init; } = true;

    public sealed class Validator : AbstractValidator<SearchIngredientsRequest>
    {
        public Validator()
        {
            RuleFor(request => request.Name).NotEmpty();
            RuleFor(request => request.Page).InclusiveBetween(0, int.MaxValue / 50);
            RuleFor(request => request.PageSize).InclusiveBetween(1, 50);
        }
    }
}
