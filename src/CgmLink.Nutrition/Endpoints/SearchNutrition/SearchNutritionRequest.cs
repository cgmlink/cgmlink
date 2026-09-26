using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace CgmLink.Nutrition.Endpoints.SearchNutrition;

public sealed record SearchNutritionRequest
{
    [Required]
    public required string Query { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; } = 20;

    public sealed class Validator : AbstractValidator<SearchNutritionRequest>
    {
        public Validator()
        {
            RuleFor(request => request.Query).NotEmpty();
            RuleFor(request => request.Page).GreaterThanOrEqualTo(0);
            RuleFor(request => request.PageSize).InclusiveBetween(1, 50);
        }
    }
}
