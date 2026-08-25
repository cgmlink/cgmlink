using CgmLink.Nutrition.Data.Entities;
using CgmLink.Nutrition.Data.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CgmLink.Nutrition.Endpoints.GetProduct;

internal static class Endpoint
{
    internal static async Task<Results<Ok<ProductResponse>, NotFound, UnauthorizedHttpResult>> HandleAsync(
        [FromRoute] string code,
        [FromServices] IRepository<Product> repository,
        [FromServices] IRepository<ProductBrand> brandRepository,
        CancellationToken cancellationToken)
    {
        var product = await repository.FindOneAsync(p => p.Code == code,
                new FindOptions() { IsAsNoTracking = true, IsIgnoreAutoIncludes = true }, cancellationToken)
            .ConfigureAwait(false);

        if (product is null)
        {
            return TypedResults.NotFound();
        }

        var brands = await brandRepository
            .Find(b => b.ProductId == product.Id, new FindOptions { IsAsNoTracking = true, IsIgnoreAutoIncludes = true })
            .Select(b => b.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var response = new ProductResponse()
        {
            Id = product.Id,
            ProductType = product.ProductType,
            Quantity = product.Quantity,
            ProductQuantityUnit = product.ProductQuantityUnit,
            ProductName = product.ProductName,
            ProductQuantity = product.ProductQuantity,
            NutritionDataPer = product.NutritionDataPer,
            Nutriments = new NutrimentsResponse()
            {
                EnergyUnit = product.Nutriments?.EnergyUnit,
                FatUnit = product.Nutriments?.FatUnit,
                CarbohydratesUnit = product.Nutriments?.CarbohydratesUnit,
                EnergyKcalUnit = product.Nutriments?.EnergyKcalUnit,
                EnergyKcalValue = product.Nutriments?.EnergyKcalValue,
                EnergyValue = product.Nutriments?.EnergyValue,
                CarbohydratesValue = product.Nutriments?.CarbohydratesValue,
                Proteins = product.Nutriments?.Proteins,
                EnergyKcalValueComputed = product.Nutriments?.EnergyKcalValueComputed,
                ProteinsValue = product.Nutriments?.ProteinsValue,
                EnergyKcal = product.Nutriments?.EnergyKcal,
                ProteinsUnit = product.Nutriments?.ProteinsUnit,
                Carbohydrates = product.Nutriments?.Carbohydrates,
                Energy = product.Nutriments?.Energy,
                Fat = product.Nutriments?.Fat,
                FatValue = product.Nutriments?.FatValue,
                Energy100g = product.Nutriments?.Energy100g,
                EnergyServing = product.Nutriments?.EnergyServing,
                EnergyKcal100g = product.Nutriments?.EnergyKcal100g,
                EnergyKcalServing = product.Nutriments?.EnergyKcalServing,
                Fat100g = product.Nutriments?.Fat100g,
                FatServing = product.Nutriments?.FatServing,
                Carbohydrates100g = product.Nutriments?.Carbohydrates100g,
                CarbohydratesServing = product.Nutriments?.CarbohydratesServing,
                Proteins100g = product.Nutriments?.Proteins100g,
                ProteinsServing = product.Nutriments?.ProteinsServing,
            },
            NutritionDataPreparedPer = product.NutritionDataPreparedPer,
            Code = product.Code,
            ServingQuantity = product.ServingQuantity,
            ImageUrl = product.ImageUrl,
            ImageThumbUrl = product.ImageThumbUrl,
            Brands = brands,
        };

        return TypedResults.Ok(response);
    }
}
