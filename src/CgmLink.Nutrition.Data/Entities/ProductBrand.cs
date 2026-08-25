using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CgmLink.Nutrition.Data.Entities;

public class ProductBrand
{
    [Key]
    public string Id { get; set; }

    [ForeignKey(nameof(Product))]
    public string ProductId { get; set; }

    public Product? Product { get; set; }

    public string Name { get; set; }
}
