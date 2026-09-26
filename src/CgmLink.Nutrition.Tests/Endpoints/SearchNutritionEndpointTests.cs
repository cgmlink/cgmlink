using CgmLink.Nutrition.Api.Endpoints.SearchNutrition;
using CgmLink.Nutrition.Source;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;

namespace CgmLink.Nutrition.Tests.Endpoints;

[TestFixture]
public class SearchNutritionEndpointTests
{
    private Mock<INutritionSourceClient> _nutritionSourceMock;
    private Mock<IValidator<SearchNutritionRequest>> _validatorMock;

    [SetUp]
    public void SetUp()
    {
        _nutritionSourceMock = new Mock<INutritionSourceClient>();
        _validatorMock = new Mock<IValidator<SearchNutritionRequest>>();
    }

    [Test]
    public async Task HandleAsync_Should_Return_Mapped_Foods_And_Use_Paging()
    {
        var request = new SearchNutritionRequest { Query = "apple", Page = 2, PageSize = 10 };
        _validatorMock.Setup(validator => validator.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _nutritionSourceMock.Setup(client => client.SearchAsync(
                "apple", 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new NutritionProduct { ProductId = "1", Name = "Apple" },
                new NutritionProduct { ProductId = "2", Name = "Apple Pie" },
            ]);

        var result = await Endpoint.HandleAsync(
            request, _validatorMock.Object, _nutritionSourceMock.Object, CancellationToken.None);

        var response = (result.Result as Ok<SearchNutritionResponse>)!.Value;
        Assert.That(response.Foods.Select(food => food.Name),
            Is.EqualTo(new[] { "Apple", "Apple Pie" }));
    }

    [Test]
    public async Task HandleAsync_Should_Return_ValidationProblem_When_Request_Is_Invalid()
    {
        var request = new SearchNutritionRequest { Query = "", Page = 0, PageSize = 20 };
        _validatorMock.Setup(validator => validator.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult
            {
                Errors = { new ValidationFailure("Query", "Query is required.") }
            });

        var result = await Endpoint.HandleAsync(
            request, _validatorMock.Object, _nutritionSourceMock.Object, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<ValidationProblem>());
        _nutritionSourceMock.Verify(client => client.SearchAsync(
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
