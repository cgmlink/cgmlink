namespace CgmLink.Nutrition.Endpoints.GetProduct;

public record NutrimentsResponse
{
    public string? EnergyUnit { get; set; }

    public string? FatUnit { get; set; }

    public string? CarbohydratesUnit { get; set; }

    public string? EnergyKcalUnit { get; set; }

    public double? EnergyKcalValue { get; set; }

    public double? EnergyValue { get; set; }

    public double? CarbohydratesValue { get; set; }

    public float? Proteins { get; set; }

    public double? EnergyKcalValueComputed { get; set; }

    public float? ProteinsValue { get; set; }

    public double? EnergyKcal { get; set; }

    public string? ProteinsUnit { get; set; }

    public double? Carbohydrates { get; set; }

    public float? Energy { get; set; }

    public float? Fat { get; set; }

    public float? FatValue { get; set; }

    public double? Energy100g { get; set; }

    public double? EnergyServing { get; set; }

    public double? EnergyKcal100g { get; set; }

    public double? EnergyKcalServing { get; set; }

    public double? Fat100g { get; set; }

    public double? FatServing { get; set; }

    public double? Carbohydrates100g { get; set; }

    public double? CarbohydratesServing { get; set; }

    public double? Proteins100g { get; set; }

    public double? ProteinsServing { get; set; }
}