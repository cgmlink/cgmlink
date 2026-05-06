using FluentValidation;
using System;

namespace CgmLink.Api.Endpoints.LibreLink.PairConnection;

public sealed record PairConnectionRequest
{
    public required Guid PatientId { get; set; }

    public class PairConnectionRequestValidator : AbstractValidator<PairConnectionRequest>
    {
        public PairConnectionRequestValidator()
        {
            RuleFor(x => x.PatientId).NotEmpty().WithMessage(Resources.ValidationMessages.PatientIdRequired);
        }
    }
}
