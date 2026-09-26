using CgmLink.AspNetCore.Exceptions;
using CgmLink.Nutrition.Source;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Nutrition.Endpoints.GetFood;

internal static class Endpoint
{
    internal static async Task<Results<Ok<GetFoodResponse>, NotFound, UnauthorizedHttpResult>> HandleAsync(
        [FromRoute] string productId,
        [FromServices] INutritionSourceClient nutritionSource,
        CancellationToken cancellationToken)
    {
        var product = await nutritionSource.GetAsync(productId, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            throw new NotFoundException("FOOD_NOT_FOUND");
        }

        return TypedResults.Ok(GetFoodResponse.ToResponse(product));
    }
}
