using CgmLink.Api.Models;
using Microsoft.Extensions.Options;

namespace CgmLink.Api.Endpoints.Injections.ListInjections;

public sealed record ListInjectionsRequest : PagedRequest
{
    public sealed class ListInjectionsValidator : PagedRequestValidator<ListInjectionsRequest>
    {
        public ListInjectionsValidator(IOptions<ApiSettings> apiSettings) : base(apiSettings)
        {
        }
    }
}