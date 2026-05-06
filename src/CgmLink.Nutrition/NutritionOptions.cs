using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace CgmLink.Nutrition;

[ExcludeFromCodeCoverage]
public sealed class NutritionOptions
{
    [Required] public string ConnectionString { get; init; } = "";
}