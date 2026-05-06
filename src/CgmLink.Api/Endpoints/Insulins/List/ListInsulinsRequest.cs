using FluentValidation;
using CgmLink.Api.Models;
using Microsoft.Extensions.Options;

namespace CgmLink.Api.Endpoints.Insulins.List;

public sealed record ListInsulinsRequest : PagedRequest
{
    public InsulinType? Type { get; init; }

    public sealed class ListInsulinsValidator : PagedRequestValidator<ListInsulinsRequest>
    {
        public ListInsulinsValidator(IOptions<ApiSettings> apiSettings) : base(apiSettings)
        {
            RuleFor(x => x.Type).IsInEnum().When(x => x.Type is not null).WithMessage(Resources.ValidationMessages.InsulinTypeInvalid);
        }
    }
}
