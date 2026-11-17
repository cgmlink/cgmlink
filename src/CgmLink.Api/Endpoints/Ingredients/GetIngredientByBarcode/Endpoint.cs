using CgmLink.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Endpoints.Ingredients.GetIngredientByBarcode;

internal static class Endpoint
{
    internal static async Task<Ok<IngredientResponse>> HandleAsync(
        [FromRoute] string barcode,
        [FromServices] IIngredientsService ingredientsService,
        CancellationToken cancellationToken)
    {
        var response = await ingredientsService.GetIngredientByBarcodeAsync(barcode, cancellationToken)
            .ConfigureAwait(false);
        return TypedResults.Ok(response);
    }
}
