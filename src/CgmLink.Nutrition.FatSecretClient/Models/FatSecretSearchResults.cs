using System.Collections.Generic;

namespace CgmLink.Nutrition.FatSecretClient.Models;

public sealed record FatSecretSearchResults(
    IReadOnlyList<FatSecretFoodSearchResult> Items,
    int? TotalResults = null,
    int? PageNumber = null,
    int? MaxResults = null);
