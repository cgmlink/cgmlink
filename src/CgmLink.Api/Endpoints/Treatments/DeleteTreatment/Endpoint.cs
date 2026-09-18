using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Endpoints.Treatments.DeleteTreatment;

internal static class Endpoint
{
    internal static async Task<Results<NoContent, NotFound, UnauthorizedHttpResult>> HandleAsync(
        [FromRoute] Guid id,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IRepository<Treatment> treatmentsRepository,
        [FromServices] IRepository<Injection> injectionsRepository,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();

        var treatment = await treatmentsRepository
            .FindOneAsync(t => t.Id == id && t.UserId == userId && t.Deleted == null, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (treatment is null)
        {
            throw new NotFoundException("TREATMENT_NOT_FOUND");
        }

        await treatmentsRepository.DeleteAsync(treatment, cancellationToken).ConfigureAwait(false);

        if (treatment.InjectionId is not null)
        {
            var injection = await injectionsRepository
                .FindOneAsync(i => i.Id == treatment.InjectionId && i.UserId == userId, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (injection is not null)
            {
                await injectionsRepository.DeleteAsync(injection, cancellationToken).ConfigureAwait(false);
            }
        }

        return TypedResults.NoContent();
    }
}