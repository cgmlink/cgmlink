using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using CgmLink.Api.Endpoints.Insights.HourlyAverageGlucose;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using NUnit.Framework;

namespace CgmLink.Api.Tests.Endpoints.Insights;

[TestFixture]
internal sealed class HourlyAverageGlucoseEndpointTests
{
    private Mock<IValidator<HourlyAverageGlucoseRequest>> _validatorMock;
    private Mock<ICurrentUser> _currentUserMock;
    private Mock<IRepository<Reading>> _repositoryMock;

    [SetUp]
    public void SetUp()
    {
        _validatorMock = new Mock<IValidator<HourlyAverageGlucoseRequest>>();
        _currentUserMock = new Mock<ICurrentUser>();
        _repositoryMock = new Mock<IRepository<Reading>>();
    }

    [Test]
    public async Task Handle_With_Valid_Request_Returns_TwentyFour_Hourly_Buckets()
    {
        var userId = Guid.NewGuid();
        _currentUserMock.Setup(cu => cu.GetUserId()).Returns(userId);

        var buckets = new List<Endpoint.HourlyAverage>
        {
            new() { Hour = 6, AverageGlucoseLevel = 110, ReadingCount = 2 },
            new() { Hour = 14, AverageGlucoseLevel = 90, ReadingCount = 1 }
        };

        _repositoryMock.Setup(r => r.FromSqlRaw<Endpoint.HourlyAverage>(It.IsAny<string>(), It.IsAny<FindOptions>(), userId, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<int>()))
            .Returns(buckets.AsQueryable());

        var request = new HourlyAverageGlucoseRequest
        {
            From = DateTimeOffset.UtcNow.AddDays(-7),
            To = DateTimeOffset.UtcNow
        };

        _validatorMock.Setup(v => v.ValidateAsync(request, CancellationToken.None))
            .ReturnsAsync(new ValidationResult());

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object, _currentUserMock.Object,
            _repositoryMock.Object, CancellationToken.None);

        Assert.Multiple(() =>
        {
            var okResult = (Ok<List<HourlyAverageGlucoseResponse>>)result.Result;
            Assert.That(okResult, Is.InstanceOf<Ok<List<HourlyAverageGlucoseResponse>>>());
            Assert.That(okResult.Value, Has.Count.EqualTo(24));

            var sixAm = okResult.Value.Single(r => r.Hour == 6);
            Assert.That(sixAm.AverageGlucoseLevel, Is.EqualTo(110));
            Assert.That(sixAm.ReadingCount, Is.EqualTo(2));

            var twoPm = okResult.Value.Single(r => r.Hour == 14);
            Assert.That(twoPm.AverageGlucoseLevel, Is.EqualTo(90));
            Assert.That(twoPm.ReadingCount, Is.EqualTo(1));

            var noReadings = okResult.Value.Single(r => r.Hour == 3);
            Assert.That(noReadings.AverageGlucoseLevel, Is.Null);
            Assert.That(noReadings.ReadingCount, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task Handle_With_No_Readings_Returns_Empty_Buckets()
    {
        _currentUserMock.Setup(cu => cu.GetUserId()).Returns(Guid.NewGuid());
        _repositoryMock.Setup(r => r.FromSqlRaw<Endpoint.HourlyAverage>(It.IsAny<string>(), It.IsAny<FindOptions>(), It.IsAny<object[]>()))
            .Returns(Enumerable.Empty<Endpoint.HourlyAverage>().AsQueryable());

        var request = new HourlyAverageGlucoseRequest
        {
            From = DateTimeOffset.UtcNow.AddDays(-7),
            To = DateTimeOffset.UtcNow
        };

        _validatorMock.Setup(v => v.ValidateAsync(request, CancellationToken.None))
            .ReturnsAsync(new ValidationResult());

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object, _currentUserMock.Object,
            _repositoryMock.Object, CancellationToken.None);

        Assert.Multiple(() =>
        {
            var okResult = (Ok<List<HourlyAverageGlucoseResponse>>)result.Result;
            Assert.That(okResult.Value, Has.Count.EqualTo(24));
            Assert.That(okResult.Value, Has.All.Matches<HourlyAverageGlucoseResponse>(r => r.AverageGlucoseLevel == null && r.ReadingCount == 0));
        });
    }

    [Test]
    public async Task Handle_With_Invalid_Request_Returns_ValidationProblem()
    {
        var request = new HourlyAverageGlucoseRequest { From = DateTimeOffset.UtcNow, To = DateTimeOffset.UtcNow.AddDays(-1) };

        _validatorMock.Setup(v => v.ValidateAsync(request, CancellationToken.None))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("From", "'To' must be after 'From'") }));

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object, _currentUserMock.Object,
            _repositoryMock.Object, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<ValidationProblem>());
    }

    [Test]
    public void Handle_With_Unauthorized_User_Throws_UnauthorizedException()
    {
        var exception = new CgmLink.AspNetCore.Exceptions.UnauthorizedException("Unauthorized",
            CgmLink.AspNetCore.Exceptions.UnauthorizedSource.CgmLink);
        _currentUserMock.Setup(cu => cu.GetUserId()).Throws(exception);

        var request = new HourlyAverageGlucoseRequest
        {
            From = DateTimeOffset.UtcNow.AddDays(-7),
            To = DateTimeOffset.UtcNow
        };

        _validatorMock.Setup(v => v.ValidateAsync(request, CancellationToken.None))
            .ReturnsAsync(new ValidationResult());

        Assert.That(() => Endpoint.HandleAsync(request, _validatorMock.Object, _currentUserMock.Object,
            _repositoryMock.Object, CancellationToken.None), Throws.InstanceOf<CgmLink.AspNetCore.Exceptions.UnauthorizedException>());
    }
}
