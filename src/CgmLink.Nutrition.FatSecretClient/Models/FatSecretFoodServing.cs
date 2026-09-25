namespace CgmLink.Nutrition.FatSecretClient.Models;

public sealed class FatSecretFoodServing
{
    public string? ExternalId { get; set; }

    public string? Description { get; set; }

    public decimal? ServingAmount { get; set; }

    public string? ServingUnit { get; set; }

    public decimal? Calories { get; set; }

    public decimal? Carbs { get; set; }

    public decimal? Protein { get; set; }

    public decimal? Fat { get; set; }
}
