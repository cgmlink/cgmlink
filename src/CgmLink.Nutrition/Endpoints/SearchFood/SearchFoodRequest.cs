using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace CgmLink.Nutrition.Endpoints.SearchFood;

public sealed record SearchFoodRequest
{
    [Required]
    public required string Query { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; } = 20;

    public sealed class Validator : AbstractValidator<SearchFoodRequest>
    {
        public Validator()
        {
            RuleFor(request => request.Query).NotEmpty();
            RuleFor(request => request.Page).GreaterThanOrEqualTo(0);
            RuleFor(request => request.PageSize).InclusiveBetween(1, 50);
        }
    }
}
