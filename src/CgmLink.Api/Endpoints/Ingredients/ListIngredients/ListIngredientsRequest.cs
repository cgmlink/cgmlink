using CgmLink.Api.Models;
using Microsoft.Extensions.Options;

namespace CgmLink.Api.Endpoints.Ingredients.ListIngredients;

public sealed record ListIngredientsRequest : PagedRequest
{
    public sealed class ListIngredientsValidator : PagedRequestValidator<ListIngredientsRequest>
    {
        public ListIngredientsValidator(IOptions<ApiSettings> apiSettings) : base(apiSettings) { }
    }
}