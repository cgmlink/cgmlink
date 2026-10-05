using CgmLink.Api.Endpoints.Ingredients.SearchIngredients;
using CgmLink.Api.Services;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Tests.Endpoints.Ingredients;

[TestFixture]
public class SearchIngredientsTests
{
    [TestCase("", 0, 20)]
    [TestCase(" ", 0, 20)]
    [TestCase("Milk", -1, 20)]
    [TestCase("Milk", int.MaxValue, 20)]
    [TestCase("Milk", 0, 0)]
    [TestCase("Milk", 0, 51)]
    public async Task InvalidSearch_ReturnsValidationProblemWithoutCallingService(string name, int page, int pageSize)
    {
        var service = new Mock<IIngredientsService>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(user => user.GetUserId()).Returns(Guid.NewGuid());

        var result = await Endpoint.HandleAsync(new SearchIngredientsRequest { Name = name, Page = page, PageSize = pageSize },
            new SearchIngredientsRequest.Validator(), currentUser.Object, service.Object, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<ValidationProblem>());
        service.VerifyNoOtherCalls();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task ValidSearch_PassesUserAndPaginationToService(bool includeExternal)
    {
        var userId = Guid.NewGuid();
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(user => user.GetUserId()).Returns(userId);
        var response = new SearchIngredientsResponse(new([], 2, 10, 0), new([], 2, 10, null));
        var service = new Mock<IIngredientsService>();
        service.Setup(item => item.SearchIngredientsAsync("Milk", userId, 2, 10, includeExternal, CancellationToken.None)).ReturnsAsync(response);

        var result = await Endpoint.HandleAsync(new SearchIngredientsRequest { Name = "Milk", Page = 2, PageSize = 10, IncludeExternal = includeExternal },
            new SearchIngredientsRequest.Validator(), currentUser.Object, service.Object, CancellationToken.None);

        Assert.That(((Ok<SearchIngredientsResponse>)result.Result).Value, Is.SameAs(response));
        service.VerifyAll();
    }

    [Test]
    public void AnonymousSearch_IsRejectedBeforeServiceCall()
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(user => user.GetUserId()).Throws<UnauthorizedAccessException>();
        var service = new Mock<IIngredientsService>(MockBehavior.Strict);

        Assert.That(async () => await Endpoint.HandleAsync(new SearchIngredientsRequest { Name = "Milk" },
            new SearchIngredientsRequest.Validator(), currentUser.Object, service.Object, CancellationToken.None),
            Throws.TypeOf<UnauthorizedAccessException>());
        service.VerifyNoOtherCalls();
    }
}
