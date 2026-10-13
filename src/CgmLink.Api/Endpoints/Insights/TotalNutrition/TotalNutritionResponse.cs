namespace CgmLink.Api.Endpoints.Insights.TotalNutrition;

public sealed record TotalNutritionResponse
{
    public decimal TotalCalories { get; init; }
    public decimal TotalCarbs { get; init; }
    public decimal TotalProtein { get; init; }
    public decimal TotalFat { get; init; }
}
