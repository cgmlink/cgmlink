using CgmLink.Api.Models;
using CgmLink.Data.Entities;
using FluentValidation;
using Microsoft.Extensions.Options;
using System.Linq;

namespace CgmLink.Api.Endpoints.Treatments.ListTreatments;

public sealed record ListTreatmentsRequest : PagedRequest
{
    internal static readonly string[] SortFields =
        [nameof(Treatment.Created), nameof(Treatment.Updated),
         nameof(Treatment.Calories), nameof(Treatment.Carbs),
         nameof(Treatment.Protein), nameof(Treatment.Fat)];

    public sealed class ListTreatmentsValidator : PagedRequestValidator<ListTreatmentsRequest>
    {
        public ListTreatmentsValidator(IOptions<ApiSettings> apiSettings) : base(apiSettings)
        {
            RuleFor(x => x.SortBy)
                .Must(field => field is not null && ListTreatmentsRequest.SortFields.Contains(field))
                .WithMessage(Resources.ValidationMessages.SortByInvalid)
                .When(x => !string.IsNullOrWhiteSpace(x.SortBy));
        }
    }
}