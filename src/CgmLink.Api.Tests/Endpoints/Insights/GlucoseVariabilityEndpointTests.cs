using FluentValidation;
using FluentValidation.Results;
using CgmLink.Api.Endpoints.Insights.GlucoseVariability;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Tests.Endpoints.Insights;

[TestFixture]
internal sealed class GlucoseVariabilityEndpointTests
{
    private Mock<IValidator<GlucoseVariabilityRequest>> _validatorMock;
    private Mock<ICurrentUser> _currentUserMock;
    private Mock<IRepository<Reading>> _repositoryMock;
    private CancellationToken _cancellationToken;

    [SetUp]
    public void SetUp()
    {
        _validatorMock = new Mock<IValidator<GlucoseVariabilityRequest>>();
        _currentUserMock = new Mock<ICurrentUser>();
        _repositoryMock = new Mock<IRepository<Reading>>();
        _cancellationToken = CancellationToken.None;
    }

    [Test]
    public async Task HandleAsync_Returns_Ok_With_Computed_CoefficientOfVariation_()
    {
        var userId = Guid.NewGuid();
        var request = new GlucoseVariabilityRequest { From = DateTimeOffset.UtcNow.AddDays(-1), To = DateTimeOffset.UtcNow };
        var validationResult = new ValidationResult();
        var stats = new List<Endpoint.VarianceStats> { new() { Mean = 8.0, StandardDeviation = 2.0 } };

        _currentUserMock.Setup(x => x.GetUserId()).Returns(userId);
        _validatorMock.Setup(x => x.ValidateAsync(request, _cancellationToken)).ReturnsAsync(validationResult);
        _repositoryMock.Setup(x => x.FromSqlRaw<Endpoint.VarianceStats>(It.IsAny<string>(), It.IsAny<FindOptions>(), userId, request.From, request.To))
            .Returns(stats.AsQueryable());

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object, _currentUserMock.Object, _repositoryMock.Object, _cancellationToken);

        Assert.That(result.Result, Is.InstanceOf<Ok<GlucoseVariabilityResponse>>());
        var okResult = (Ok<GlucoseVariabilityResponse>)result.Result;
        Assert.That(okResult.Value.StandardDeviation, Is.EqualTo(2.0));
        Assert.That(okResult.Value.CoefficientOfVariation, Is.EqualTo(25.0));
    }

    [Test]
    public async Task HandleAsync_Returns_Zero_CoefficientOfVariation_When_No_Readings_()
    {
        var userId = Guid.NewGuid();
        var request = new GlucoseVariabilityRequest();
        var validationResult = new ValidationResult();

        _currentUserMock.Setup(x => x.GetUserId()).Returns(userId);
        _validatorMock.Setup(x => x.ValidateAsync(It.IsAny<GlucoseVariabilityRequest>(), _cancellationToken)).ReturnsAsync(validationResult);
        _repositoryMock.Setup(x => x.FromSqlRaw<Endpoint.VarianceStats>(It.IsAny<string>(), It.IsAny<FindOptions>(), It.IsAny<object[]>()))
            .Returns(Enumerable.Empty<Endpoint.VarianceStats>().AsQueryable());

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object, _currentUserMock.Object, _repositoryMock.Object, _cancellationToken);

        Assert.That(result.Result, Is.InstanceOf<Ok<GlucoseVariabilityResponse>>());
        var okResult = (Ok<GlucoseVariabilityResponse>)result.Result;
        Assert.That(okResult.Value.StandardDeviation, Is.EqualTo(0));
        Assert.That(okResult.Value.CoefficientOfVariation, Is.EqualTo(0));
    }

    [Test]
    public async Task HandleAsync_Returns_ValidationProblem_When_Request_Is_Invalid_()
    {
        var userId = Guid.NewGuid();
        var request = new GlucoseVariabilityRequest();
        var validationResult = new ValidationResult(new[] { new ValidationFailure("From", "Required") });

        _currentUserMock.Setup(x => x.GetUserId()).Returns(userId);
        _validatorMock.Setup(x => x.ValidateAsync(request, _cancellationToken)).ReturnsAsync(validationResult);

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object, _currentUserMock.Object, _repositoryMock.Object, _cancellationToken);

        Assert.That(result.Result, Is.InstanceOf<ValidationProblem>());
    }
}
