using FluentValidation;
using CgmLink.Api.Models;
using CgmLink.Data.Entities;
using CgmLink.Data.Extensions;
using CgmLink.Data.Repository;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Endpoints.Treatments.ListTreatments;

internal static class Endpoint
{
    internal static async Task<Results<Ok<ListTreatmentsResponse>, UnauthorizedHttpResult, ValidationProblem>> HandleAsync(
        [AsParameters] ListTreatmentsRequest request,
        [FromServices] IValidator<ListTreatmentsRequest> validator,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IRepository<Treatment> treatmentsRepository,
        CancellationToken cancellationToken)
    {
        if (await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false) is
            { IsValid: false } validation)
        {
            return TypedResults.ValidationProblem(validation.ToDictionary());
        }

        var userId = currentUser.GetUserId();
        var sortBy = string.IsNullOrWhiteSpace(request.SortBy) ? nameof(Treatment.Created) : request.SortBy;
        var descending = (request.SortDirection ?? SortDirection.Desc) == SortDirection.Desc;

        var treatments = await treatmentsRepository.Find(t => t.UserId == userId && t.Deleted == null, new FindOptions { IsAsNoTracking = true })
            .Include(t => t.Reading)
            .Include(t => t.Injection)
                .ThenInclude(i => i.Insulin)
            .OrderByProperty(sortBy, descending)
            .Skip(request.Page * request.PageSize)
            .Take(request.PageSize)
            .Select(t => ListTreatmentResponse.ToResponse(t))
            .ToListAsync(cancellationToken);

        var totalTreatments = await treatmentsRepository.CountAsync(t => t.UserId == userId && t.Deleted == null, cancellationToken).ConfigureAwait(false);
        var numberOfPages = (int)Math.Ceiling(totalTreatments / (double)request.PageSize);

        var response = new ListTreatmentsResponse
        {
            Treatments = treatments,
            NumberOfPages = numberOfPages,
        };

        return TypedResults.Ok(response);
    }
}