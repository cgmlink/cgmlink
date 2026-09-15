using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Api.Endpoints.Meals.DeleteMeal;
using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Data.Tests;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using NUnit.Framework;

namespace CgmLink.Api.Tests.Endpoints.Meals.DeleteMeal;

[TestFixture]
public class DeleteMealTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private Mock<IRepository<Meal>> _mealsRepositoryMock;
    private Mock<ICurrentUser> _currentUserMock;

    [SetUp]
    public void SetUp()
    {
        _mealsRepositoryMock = new Mock<IRepository<Meal>>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(c => c.GetUserId()).Returns(_userId);

        _mealsRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Meal>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private Meal CreateMeal(Guid id)
    {
        return new Meal
        {
            Id = id,
            UserId = _userId,
            Name = "Breakfast",
            ImageUrl = "https://example.com/image.jpg",
            ThumbnailUrl = "https://example.com/thumb.jpg",
            Calories = 500,
            Carbs = 50,
            Protein = 25,
            Fat = 20,
            Created = DateTimeOffset.UtcNow.AddDays(-1),
        };
    }

    private void SetupMeal(Meal meal)
    {
        _mealsRepositoryMock
            .Setup(r => r.GetAll())
            .Returns(new TestAsyncEnumerable<Meal>(new List<Meal> { meal }));
    }

    [Test]
    public async Task HandleAsync_Should_Return_NoContent_When_Meal_Is_Deleted()
    {
        var id = Guid.NewGuid();
        var meal = CreateMeal(id);
        SetupMeal(meal);

        var result = await Endpoint.HandleAsync(id, _currentUserMock.Object,
            _mealsRepositoryMock.Object, CancellationToken.None);

        _mealsRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Meal>(m =>
            m.Deleted != null
        ), It.IsAny<CancellationToken>()), Times.Once);

        Assert.That(result.Result, Is.TypeOf<NoContent>());
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Meal_Not_Found()
    {
        var id = Guid.NewGuid();

        _mealsRepositoryMock
            .Setup(r => r.GetAll())
            .Returns(new TestAsyncEnumerable<Meal>(new List<Meal>()));

        Assert.That(async () => await Endpoint.HandleAsync(id, _currentUserMock.Object,
                _mealsRepositoryMock.Object, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("MEAL_NOT_FOUND"));
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Meal_Not_Linked_To_User()
    {
        var id = Guid.NewGuid();
        var meal = new Meal
        {
            Id = id,
            UserId = Guid.NewGuid(),
            Name = "Breakfast",
            Calories = 500,
            Carbs = 50,
            Protein = 25,
            Fat = 20,
            Created = DateTimeOffset.UtcNow,
        };
        SetupMeal(meal);

        Assert.That(async () => await Endpoint.HandleAsync(id, _currentUserMock.Object,
                _mealsRepositoryMock.Object, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("MEAL_NOT_FOUND"));
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Meal_Is_Already_Soft_Deleted()
    {
        var id = Guid.NewGuid();
        var meal = CreateMeal(id);
        meal.Deleted = DateTimeOffset.UtcNow;
        SetupMeal(meal);

        Assert.That(async () => await Endpoint.HandleAsync(id, _currentUserMock.Object,
                _mealsRepositoryMock.Object, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("MEAL_NOT_FOUND"));

        _mealsRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Meal>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void HandleAsync_Should_Throw_UnauthorizedAccessException_When_User_Is_Not_Logged_In()
    {
        _currentUserMock
            .Setup(c => c.GetUserId())
            .Throws<UnauthorizedAccessException>();

        Assert.That(async () => await Endpoint.HandleAsync(Guid.NewGuid(), _currentUserMock.Object,
                _mealsRepositoryMock.Object, CancellationToken.None),
            Throws.InstanceOf<UnauthorizedAccessException>());
    }
}