using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using CgmLink.Api.Endpoints.Insulins.NewInsulin;
using CgmLink.Api.Models;
using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using NUnit.Framework;

namespace CgmLink.Api.Tests.Endpoints.Insulins;

[TestFixture]
public class NewInsulinTests
{
    private Mock<IValidator<NewInsulinRequest>> _validatorMock;
    private Mock<IRepository<Insulin>> _repositoryMock;
    private Mock<ICurrentUser> _currentUserMock;

    [SetUp]
    public void SetUp()
    {
        _validatorMock = new Mock<IValidator<NewInsulinRequest>>();
        _repositoryMock = new Mock<IRepository<Insulin>>();
        _currentUserMock = new Mock<ICurrentUser>();
    }

    [Test]
    public async Task HandleAsync_Should_Return_ValidationProblem_When_Request_Is_Invalid()
    {
        var request = new NewInsulinRequest { Name = "Test", Type = InsulinType.Bolus };
        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult
            {
                Errors = { new FluentValidation.Results.ValidationFailure("Name", "Name is required.") }
            });

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object, _repositoryMock.Object, _currentUserMock.Object, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<ValidationProblem>());
    }

    [Test]
    public void HandleAsync_Should_Throw_UnauthorizedException_When_User_Is_Not_Logged_In()
    {
        var request = new NewInsulinRequest
        {
            Name = "Test Insulin",
            Type = InsulinType.Bolus,
            Duration = 4.5,
            Scale = 1.2,
            PeakTime = 2.0
        };

        _currentUserMock
            .Setup(c => c.GetUserId())
            .Throws(new UnauthorizedException("USER_NOT_LOGGED_IN"));

        Assert.That(async () => await Endpoint.HandleAsync(request, _validatorMock.Object, _repositoryMock.Object, _currentUserMock.Object, CancellationToken.None),
            Throws.InstanceOf<UnauthorizedException>().With.Message.EqualTo("USER_NOT_LOGGED_IN"));
    }

    [Test]
    public async Task HandleAsync_Should_Return_Created_When_Request_Is_Valid()
    {
        var userId = Guid.NewGuid();
        _currentUserMock
            .Setup(c => c.GetUserId())
            .Returns(userId);

        var request = new NewInsulinRequest
        {
            Name = "Test Insulin",
            Type = InsulinType.Bolus,
            Duration = 4.5,
            Scale = 1.2,
            PeakTime = 2.0
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _currentUserMock
            .Setup(c => c.GetUserId())
            .Returns(userId);

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object, _repositoryMock.Object, _currentUserMock.Object, CancellationToken.None);

        _repositoryMock.Verify(r => r.AddAsync(It.Is<Insulin>(i =>
            i.Name == request.Name &&
            i.Type == (Data.Enums.InsulinType)request.Type &&
            i.Duration == request.Duration &&
            i.Scale == request.Scale &&
            i.UserId == userId
        ), It.IsAny<CancellationToken>()), Times.Once);

        Assert.That(result.Result, Is.TypeOf<Created<NewInsulinResponse>>());
        var okResult = result.Result as Created<NewInsulinResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult!.Value.Name, Is.EqualTo(request.Name));
            Assert.That(okResult.Value.Type, Is.EqualTo(request.Type));
            Assert.That(okResult.Value.Duration, Is.EqualTo(request.Duration));
            Assert.That(okResult.Value.Scale, Is.EqualTo(request.Scale));
            Assert.That(okResult.Value.Created, Is.EqualTo(DateTimeOffset.UtcNow).Within(TimeSpan.FromSeconds(1)));
            Assert.That(okResult.Value.Id, Is.Not.EqualTo(Guid.Empty));
        });
    }
}