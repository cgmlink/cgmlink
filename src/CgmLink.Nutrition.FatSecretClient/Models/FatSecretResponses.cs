using System.Collections.Generic;
using System.Text.Json.Serialization;
using CgmLink.Nutrition.FatSecretClient.Json;

namespace CgmLink.Nutrition.FatSecretClient.Models;

internal sealed class FoodSearchResponse
{
    public FoodSearchResultData? Foods { get; set; }
}

internal sealed class FoodSearchResultData
{
    [JsonConverter(typeof(SingleOrArrayConverter<FoodSearchItem>))]
    public IReadOnlyList<FoodSearchItem> Food { get; set; } = [];

    public int? MaxResults { get; set; }

    public int? PageNumber { get; set; }

    public int? TotalResults { get; set; }
}

internal sealed class FoodSearchItem
{
    public string? FoodId { get; set; }

    public string? FoodName { get; set; }
}

internal sealed class FoodGetResponse
{
    public FoodDetails? Food { get; set; }
}

internal sealed class FoodDetails
{
    public string? FoodId { get; set; }

    public string? FoodName { get; set; }

    public FoodServingsPayload? Servings { get; set; }
}

internal sealed class FoodServingsPayload
{
    [JsonConverter(typeof(SingleOrArrayConverter<FoodServingResponse>))]
    public IReadOnlyList<FoodServingResponse> Serving { get; set; } = [];
}

internal sealed class FoodServingResponse
{
    public string? ServingId { get; set; }

    public string? ServingDescription { get; set; }

    public decimal? MetricServingAmount { get; set; }

    public string? MetricServingUnit { get; set; }

    public decimal? Calories { get; set; }

    public decimal? Carbohydrate { get; set; }

    public decimal? Protein { get; set; }

    public decimal? Fat { get; set; }
}
