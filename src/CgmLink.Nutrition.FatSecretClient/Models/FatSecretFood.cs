using System.Collections.Generic;

namespace CgmLink.Nutrition.FatSecretClient.Models;

public sealed class FatSecretFood
{
    public string ProductId { get; set; } = "";

    public string Name { get; set; } = "";

    public IReadOnlyList<FatSecretFoodServing> Servings { get; set; } = [];
}
