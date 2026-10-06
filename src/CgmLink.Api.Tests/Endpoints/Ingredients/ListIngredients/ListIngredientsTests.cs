using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Api.Endpoints.Ingredients;
using CgmLink.Api.Endpoints.Ingredients.ListIngredients;
using CgmLink.Api.Services;
using CgmLink.Identity.Authentication;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using Endpoint = CgmLink.Api.Endpoints.Ingredients.ListIngredients.Endpoint;

namespace CgmLink.Api.Tests.Endpoints.Ingredients;

[TestFixture]
public class ListIngredientsTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private Mock<IValidator<ListIngredientsRequest>> _validatorMock;
    private Mock<IIngredientsService> _ingredientsServiceMock;
    private Mock<ICurrentUser> _currentUserMock;

    [SetUp]
    public void SetUp()
    {
        _validatorMock = new Mock<IValidator<ListIngredientsRequest>>();
        _ingredientsServiceMock = new Mock<IIngredientsService>(MockBehavior.Strict);
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(c => c.GetUserId()).Returns(_userId);
    }

    [Test]
    public async Task HandleAsync_Should_Return_ValidationProblem_When_Request_Is_Invalid()
    {
        var request = new ListIngredientsRequest { Page = 0, PageSize = 10 };
        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult
            {
                Errors = { new ValidationFailure("Page", "Page is required.") }
            });

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _currentUserMock.Object, _ingredientsServiceMock.Object, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<ValidationProblem>());
        _ingredientsServiceMock.VerifyNoOtherCalls();
    }

    [Test]
    public void HandleAsync_Should_Throw_UnauthorizedException_When_User_Is_Not_Logged_In()
    {
        var request = new ListIngredientsRequest { Page = 0, PageSize = 10 };
        _currentUserMock.Setup(c => c.GetUserId()).Throws<UnauthorizedAccessException>();

        Assert.That(async () => await Endpoint.HandleAsync(request, _validatorMock.Object,
                _currentUserMock.Object, _ingredientsServiceMock.Object, CancellationToken.None),
            Throws.InstanceOf<UnauthorizedAccessException>());
        _ingredientsServiceMock.VerifyNoOtherCalls();
    }

    [TestCase(null)]
    [TestCase(IngredientType.Personal)]
    [TestCase(IngredientType.External)]
    public async Task HandleAsync_Should_Return_Service_Response_For_Selected_Type(IngredientType? type)
    {
        var request = new ListIngredientsRequest { Type = type, Name = "Milk", Page = 2, PageSize = 10 };
        var response = new ListIngredientsResponse { Ingredients = [], NumberOfPages = null };
        if (type == IngredientType.External)
        {
            _ingredientsServiceMock.Setup(service => service.SearchExternalIngredientsAsync("Milk", 2, 10, CancellationToken.None))
                .ReturnsAsync(response.Ingredients);
        }
        else
        {
            _ingredientsServiceMock.Setup(service => service.ListPersonalIngredientsAsync(
                    _userId, 2, 10, "Milk", null, null, CancellationToken.None))
                .ReturnsAsync(response);
        }

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _currentUserMock.Object, _ingredientsServiceMock.Object, CancellationToken.None);

        Assert.That(((Ok<ListIngredientsResponse>)result.Result).Value, Is.EqualTo(response));
        _ingredientsServiceMock.VerifyAll();
    }

    [TestCase("0", IngredientType.Personal)]
    [TestCase("External", IngredientType.External)]
    [TestCase("Personal", IngredientType.Personal)]
    [TestCase("1", IngredientType.External)]
    [TestCase(null, IngredientType.Personal)]
    public async Task HandleAsync_Should_Bind_Ingredient_Type_From_Query(string value, IngredientType expected)
    {
        using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        IngredientType? selected = null;
        var handler = RequestDelegateFactory.Create(([AsParameters] ListIngredientsRequest request) =>
        {
            selected = request.Type ?? IngredientType.Personal;
        }, new RequestDelegateFactoryOptions { ServiceProvider = services }).RequestDelegate;
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.QueryString = new QueryString("?page=0&pageSize=20" + (value is null ? "" : "&type=" + value));
        context.Response.Body = new MemoryStream();

        await handler(context);

        Assert.That(context.Response.StatusCode, Is.EqualTo(200));
        Assert.That(selected, Is.EqualTo(expected));
    }
}
