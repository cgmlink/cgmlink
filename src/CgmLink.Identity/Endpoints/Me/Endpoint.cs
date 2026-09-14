using System.Threading;
using System.Threading.Tasks;
using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Identity.Authentication;
using CgmLink.Identity.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CgmLink.Identity.Endpoints.Me;

internal static class Endpoint
{
    internal static async Task<Results<Ok<MeResponse>, UnauthorizedHttpResult>> HandleAsync(
        [FromServices] ICurrentUser currentUser,
        [FromServices] IRepository<User> userRepository,
        [FromServices] IOptions<IdentityOptions> identityOptions,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();

        var user = await userRepository.FindOneAsync(s => s.Id == userId,
                new FindOptions { IsAsNoTracking = false, IsIgnoreAutoIncludes = true }, cancellationToken)
            .ConfigureAwait(false);
        if (user is null)
        {
            throw new UnauthorizedException("USER_NOT_LOGGED_IN", UnauthorizedSource.CgmLink);
        }

        var response = new MeResponse(
            user.Id,
            user.Email,
            !identityOptions.Value.RequireEmailVerification || user.IsVerified,
            user is Patient ? UserType.Patient : UserType.Unknown,
            user is Patient patient ? (GlucoseProvider?)patient.GlucoseProvider : null
        );

        return TypedResults.Ok(response);
    }
}