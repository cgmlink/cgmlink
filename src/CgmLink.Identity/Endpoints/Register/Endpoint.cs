using FluentValidation;
using CgmLink.Identity.Models;
using CgmLink.Identity.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Identity.Endpoints.Register;

internal static class Endpoint
{
    internal static async Task<Results<Ok<RegisterResponse>, ValidationProblem>> HandleAsync(
        [FromBody] RegisterRequest request,
        [FromServices] IValidator<RegisterRequest> validator,
        [FromServices] IUserService userService,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false) is
            { IsValid: false } validation)
        {
            return TypedResults.ValidationProblem(validation.ToDictionary());
        }

        var response = await userService.RegisterAsync(request, cancellationToken)
            .ConfigureAwait(false);
        return TypedResults.Ok(response);
    }
}