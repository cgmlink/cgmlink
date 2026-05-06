using CgmLink.Api.Endpoints.Meals.List;
using CgmLink.Api.Models;
using Microsoft.Extensions.Options;

namespace CgmLink.Api.Endpoints.Pens.ListPens;

public sealed record ListPensRequest : PagedRequest
{
    public sealed class ListPensValidator : PagedRequestValidator<ListPensRequest>
    {
        public ListPensValidator(IOptions<ApiSettings> apiSettings) : base(apiSettings) { }
    }
}
