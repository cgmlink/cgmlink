using CgmLink.Nutrition.Data.Entities;
using CgmLink.Nutrition.Data.Importer;
using Newtonsoft.Json;

namespace CgmLink.Nutrition.Tests.Importer;

[TestFixture]
public class ImportRunnerTests
{
    [Test]
    public void MapProduct_Maps_Image_Urls()
    {
        var source = new OpenFoodFactsProduct
        {
            Code = "123",
            ProductName = "Test Product",
            ImageUrlValue = "https://images.example/full.jpg",
            ImageThumbUrlValue = "https://images.example/thumb.jpg",
            Nutriments = new OpenFoodFactsNutriments()
        };

        var product = ImportRunner.MapProduct(source);

        Assert.That(product.Code, Is.EqualTo(source.Code));
        Assert.That(product.ImageUrl, Is.EqualTo(source.ImageUrl));
        Assert.That(product.ImageThumbUrl, Is.EqualTo(source.ImageThumbUrl));
    }

    [Test]
    public void ApplyBackfill_Updates_Image_Fields_When_Source_Has_Values()
    {
        var product = new Product
        {
            ImageUrl = "https://images.example/original-full.jpg",
            ImageThumbUrl = "https://images.example/original-thumb.jpg"
        };

        ImportRunner.ApplyBackfill(product, new ProductImageData("https://images.example/full.jpg", "https://images.example/thumb.jpg"), overwriteMissingImages: false);

        Assert.That(product.ImageUrl, Is.EqualTo("https://images.example/full.jpg"));
        Assert.That(product.ImageThumbUrl, Is.EqualTo("https://images.example/thumb.jpg"));
    }

    [Test]
    public void ApplyBackfill_Does_Not_Clear_Existing_Image_Fields_By_Default()
    {
        var product = new Product
        {
            ImageUrl = "https://images.example/original-full.jpg",
            ImageThumbUrl = "https://images.example/original-thumb.jpg"
        };

        ImportRunner.ApplyBackfill(product, new ProductImageData(null, null), overwriteMissingImages: false);

        Assert.That(product.ImageUrl, Is.EqualTo("https://images.example/original-full.jpg"));
        Assert.That(product.ImageThumbUrl, Is.EqualTo("https://images.example/original-thumb.jpg"));
    }

    [Test]
    public void ApplyBackfill_Clears_Existing_Image_Fields_When_Overwrite_Is_Enabled()
    {
        var product = new Product
        {
            ImageUrl = "https://images.example/original-full.jpg",
            ImageThumbUrl = "https://images.example/original-thumb.jpg"
        };

        ImportRunner.ApplyBackfill(product, new ProductImageData(null, null), overwriteMissingImages: true);

        Assert.That(product.ImageUrl, Is.Null);
        Assert.That(product.ImageThumbUrl, Is.Null);
    }

    [Test]
    public void Image_Properties_Fall_Back_To_Pascal_Case_Export_Fields()
    {
        var source = JsonConvert.DeserializeObject<OpenFoodFactsProduct>(
            """
            {
              "code": "123",
              "ImageUrl": "https://images.example/full.jpg",
              "ImageThumbUrl": "https://images.example/thumb.jpg"
            }
            """);

        Assert.That(source, Is.Not.Null);
        Assert.That(source!.ImageUrl, Is.EqualTo("https://images.example/full.jpg"));
        Assert.That(source.ImageThumbUrl, Is.EqualTo("https://images.example/thumb.jpg"));
    }
}
