using CgmLink.Nutrition.Data.Entities;
using CgmLink.Nutrition.Data.Repository;
using CgmLink.Nutrition.Endpoints.GetProduct;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Nutrition.Tests.Endpoints.GetProduct;

[TestFixture]
public class GetProductEndpointTests
{
    [Test]
    public async Task HandleAsync_Returns_Ok_When_Product_Found()
    {
        var code = "123";
        var product = new Product
        {
            Id = "1",
            Code = code,
            ProductName = "Test Product",
            ImageUrl = "https://images.example/full.jpg",
            ImageThumbUrl = "https://images.example/thumb.jpg",
            Nutriments = new Nutriments()
        };
        var repoMock = new Mock<IRepository<Product>>();
        repoMock.Setup(r => r.FindOneAsync(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<Product, bool>>>(),
            It.IsAny<FindOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var brandRepoMock = new Mock<IRepository<ProductBrand>>();
        brandRepoMock.Setup(r => r.Find(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<ProductBrand, bool>>>(),
                It.IsAny<FindOptions>()))
            .Returns(new CgmLink.Data.Tests.TestAsyncEnumerable<ProductBrand>([]));

        var result = await Endpoint.HandleAsync(code, repoMock.Object, brandRepoMock.Object, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<Ok<ProductResponse>>());
        var okResult = result.Result as Ok<ProductResponse>;
        Assert.That(okResult!.Value.Id, Is.EqualTo(product.Id));
        Assert.That(okResult.Value.ProductName, Is.EqualTo(product.ProductName));
        Assert.That(okResult.Value.Code, Is.EqualTo(product.Code));
        Assert.That(okResult.Value.ImageUrl, Is.EqualTo(product.ImageUrl));
        Assert.That(okResult.Value.ImageThumbUrl, Is.EqualTo(product.ImageThumbUrl));
    }

    [Test]
    public async Task HandleAsync_Returns_NotFound_When_Product_Not_Found()
    {
        var code = "notfound";
        var repoMock = new Mock<IRepository<Product>>();
        repoMock.Setup(r => r.FindOneAsync(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<Product, bool>>>(),
            It.IsAny<FindOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var brandRepoMock = new Mock<IRepository<ProductBrand>>();

        var result = await Endpoint.HandleAsync(code, repoMock.Object, brandRepoMock.Object, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<NotFound>());
    }
}
