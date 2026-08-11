using System.Collections.Generic;
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
            LanguageCode = "en",
            ProductName = "Test Product",
            Images = new Dictionary<string, OpenFoodFactsImage>
            {
                ["front_en"] = new() { Rev = "4" }
            },
            Nutriments = new OpenFoodFactsNutriments()
        };

        var product = ImportRunner.MapProduct(source);

        Assert.That(product.Code, Is.EqualTo(source.Code));
        Assert.That(product.ImageUrl, Is.EqualTo("https://images.openfoodfacts.org/images/products/000/000/000/0123/front_en.4.full.jpg"));
        Assert.That(product.ImageThumbUrl, Is.EqualTo("https://images.openfoodfacts.org/images/products/000/000/000/0123/front_en.4.100.jpg"));
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
    public void Image_Properties_Use_First_Available_Front_Image_When_Language_Does_Not_Match()
    {
        var source = JsonConvert.DeserializeObject<OpenFoodFactsProduct>(
            """
            {
              "code": "3168930010883",
              "lc": "de",
              "images": {
                "front_en": {
                  "rev": "7"
                }
              }
            }
            """);

        Assert.That(source, Is.Not.Null);
        Assert.That(source!.ImageUrl, Is.EqualTo("https://images.openfoodfacts.org/images/products/316/893/001/0883/front_en.7.full.jpg"));
        Assert.That(source.ImageThumbUrl, Is.EqualTo("https://images.openfoodfacts.org/images/products/316/893/001/0883/front_en.7.100.jpg"));
    }

    [Test]
    public void ComputeProductImageFolder_Pads_Short_Codes_To_Thirteen_Digits()
    {
        var folder = OpenFoodFactsProduct.ComputeProductImageFolder("123");

        Assert.That(folder, Is.EqualTo("000/000/000/0123"));
    }

    [Test]
    public void ComputeProductImageFolder_Splits_Long_Codes_Into_Off_Subfolders()
    {
        var folder = OpenFoodFactsProduct.ComputeProductImageFolder("3435660768163");

        Assert.That(folder, Is.EqualTo("343/566/076/8163"));
    }

    [Test]
    public void Image_Properties_Return_Null_When_Selected_Front_Image_Is_Not_Available()
    {
        var source = new OpenFoodFactsProduct
        {
            Code = "123",
            LanguageCode = "en",
            Images = new Dictionary<string, OpenFoodFactsImage>
            {
                ["ingredients_en"] = new() { Rev = "3" }
            }
        };

        Assert.That(source.ImageUrl, Is.Null);
        Assert.That(source.ImageThumbUrl, Is.Null);
    }
}
