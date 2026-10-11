using CgmLink.Api.Services;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Endpoints.Treatments.GetTreatment;

internal static class Endpoint
{
    internal static async Task<Results<Ok<GetTreatmentResponse>, NotFound, UnauthorizedHttpResult>> HandleAsync(
        [FromRoute] Guid id,
        [FromServices] ICurrentUser currentUser,
        [FromServices] ITreatmentService treatmentService,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();

        var response = await treatmentService.GetTreatmentAsync(id, userId, cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(response);
    }
}
