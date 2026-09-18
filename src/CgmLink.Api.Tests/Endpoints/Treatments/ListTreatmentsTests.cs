using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Api.Endpoints.Treatments.ListTreatments;
using CgmLink.Data.Entities;
using CgmLink.Data.Enums;
using CgmLink.Data.Repository;
using CgmLink.Data.Tests;
using CgmLink.Identity.Authentication;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using NUnit.Framework;
using SortDirection = CgmLink.Api.Models.SortDirection;

namespace CgmLink.Api.Tests.Endpoints.Treatments;

[TestFixture]
public class ListTreatmentsTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private Mock<IValidator<ListTreatmentsRequest>> _validatorMock;
    private Mock<IRepository<Treatment>> _treatmentsRepositoryMock;
    private Mock<ICurrentUser> _currentUserMock;

    [SetUp]
    public void SetUp()
    {
        _validatorMock = new Mock<IValidator<ListTreatmentsRequest>>();
        _treatmentsRepositoryMock = new Mock<IRepository<Treatment>>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(c => c.GetUserId()).Returns(_userId);
    }

    private static Treatment CreateTreatment(
        double glucoseLevel = 7.2,
        decimal injectionUnits = 6m,
        string insulinName = "Fiasp",
        decimal? calories = null,
        DateTimeOffset? created = null,
        DateTimeOffset? updated = null)
    {
        var reading = new Reading
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Created = DateTimeOffset.UtcNow,
            GlucoseLevel = glucoseLevel,
            Direction = ReadingDirection.Steady,
        };

        var insulin = new Insulin
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = insulinName,
            Type = InsulinType.Bolus,
            Created = DateTimeOffset.UtcNow,
        };

        var injection = new Injection
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            InsulinId = insulin.Id,
            Insulin = insulin,
            Units = injectionUnits,
            Created = DateTimeOffset.UtcNow,
        };

        return new Treatment
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ReadingId = reading.Id,
            Reading = reading,
            InjectionId = injection.Id,
            Injection = injection,
            Calories = calories ?? 300m,
            Carbs = 30m,
            Protein = 15m,
            Fat = 6m,
            Created = created ?? DateTimeOffset.UtcNow,
            Updated = updated,
        };
    }

    [Test]
    public async Task HandleAsync_Should_Return_ValidationProblem_When_Request_Is_Invalid()
    {
        var request = new ListTreatmentsRequest { Page = 0, PageSize = 10 };

        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult
            {
                Errors = { new ValidationFailure("Page", "Page is required.") }
            });

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _currentUserMock.Object, _treatmentsRepositoryMock.Object, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<ValidationProblem>());
    }

    [Test]
    public void HandleAsync_Should_Throw_UnauthorizedException_When_User_Is_Not_Logged_In()
    {
        var request = new ListTreatmentsRequest { Page = 0, PageSize = 10 };

        _currentUserMock
            .Setup(c => c.GetUserId())
            .Throws<UnauthorizedAccessException>();

        Assert.That(async () => await Endpoint.HandleAsync(request, _validatorMock.Object,
                _currentUserMock.Object, _treatmentsRepositoryMock.Object, CancellationToken.None),
            Throws.InstanceOf<UnauthorizedAccessException>());
    }

    [Test]
    public async Task HandleAsync_Should_Return_Ok_With_Treatments_When_Request_Is_Valid()
    {
        var created = DateTimeOffset.UtcNow.AddDays(-1);
        var updated = DateTimeOffset.UtcNow.AddHours(-1);
        var treatment = CreateTreatment(
            glucoseLevel: 9.5,
            injectionUnits: 8m,
            insulinName: "Novorapid",
            created: created,
            updated: updated);

        _treatmentsRepositoryMock
            .Setup(r => r.Find(It.IsAny<Expression<Func<Treatment, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Treatment>(new[] { treatment }));

        _treatmentsRepositoryMock
            .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Treatment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var request = new ListTreatmentsRequest { Page = 0, PageSize = 10 };

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _currentUserMock.Object, _treatmentsRepositoryMock.Object, CancellationToken.None);

        _treatmentsRepositoryMock.Verify(r => r.Find(It.IsAny<Expression<Func<Treatment, bool>>>(), It.Is<FindOptions>(o => o.IsAsNoTracking)), Times.Once);

        Assert.That(result.Result, Is.TypeOf<Ok<ListTreatmentsResponse>>());
        var okResult = result.Result as Ok<ListTreatmentsResponse>;
        var response = okResult!.Value.Treatments.Single();
        Assert.Multiple(() =>
        {
            Assert.That(response.Id, Is.EqualTo(treatment.Id));
            Assert.That(response.Created, Is.EqualTo(created));
            Assert.That(response.Updated, Is.EqualTo(updated));
            Assert.That(response.Calories, Is.EqualTo(300m));
            Assert.That(response.Carbs, Is.EqualTo(30m));
            Assert.That(response.Protein, Is.EqualTo(15m));
            Assert.That(response.Fat, Is.EqualTo(6m));
            Assert.That(response.GlucoseLevel, Is.EqualTo(9.5));
            Assert.That(response.InjectionUnits, Is.EqualTo(8m));
            Assert.That(response.InsulinName, Is.EqualTo("Novorapid"));
            Assert.That(okResult.Value.NumberOfPages, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task HandleAsync_Should_Return_Null_Reading_And_Injection_When_Not_Present()
    {
        var treatment = CreateTreatment();
        treatment.Reading = null;
        treatment.ReadingId = null;
        treatment.Injection = null;
        treatment.InjectionId = null;

        _treatmentsRepositoryMock
            .Setup(r => r.Find(It.IsAny<Expression<Func<Treatment, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Treatment>(new[] { treatment }));

        _treatmentsRepositoryMock
            .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Treatment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var request = new ListTreatmentsRequest { Page = 0, PageSize = 10 };

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _currentUserMock.Object, _treatmentsRepositoryMock.Object, CancellationToken.None);

        var okResult = result.Result as Ok<ListTreatmentsResponse>;
        var response = okResult!.Value.Treatments.Single();
        Assert.Multiple(() =>
        {
            Assert.That(response.GlucoseLevel, Is.Null);
            Assert.That(response.InjectionUnits, Is.Null);
            Assert.That(response.InsulinName, Is.Null);
        });
    }

    [Test]
    public async Task HandleAsync_Should_Return_Empty_Treatments_When_No_Treatments_Exist()
    {
        _treatmentsRepositoryMock
            .Setup(r => r.Find(It.IsAny<Expression<Func<Treatment, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Treatment>(Enumerable.Empty<Treatment>()));

        _treatmentsRepositoryMock
            .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Treatment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var request = new ListTreatmentsRequest { Page = 0, PageSize = 10 };

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _currentUserMock.Object, _treatmentsRepositoryMock.Object, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<Ok<ListTreatmentsResponse>>());
        var okResult = result.Result as Ok<ListTreatmentsResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult!.Value.Treatments, Is.Empty);
            Assert.That(okResult.Value.NumberOfPages, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task HandleAsync_Should_Exclude_Soft_Deleted_Treatments()
    {
        var deleted = CreateTreatment();
        deleted.Deleted = DateTimeOffset.UtcNow;

        Expression<Func<Treatment, bool>> predicate = null;

        _treatmentsRepositoryMock
            .Setup(r => r.Find(It.IsAny<Expression<Func<Treatment, bool>>>(), It.IsAny<FindOptions>()))
            .Callback<Expression<Func<Treatment, bool>>, FindOptions>((expression, _) => predicate = expression)
            .Returns(new TestAsyncEnumerable<Treatment>(new[] { deleted }));

        _treatmentsRepositoryMock
            .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Treatment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var request = new ListTreatmentsRequest { Page = 0, PageSize = 10 };

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _currentUserMock.Object, _treatmentsRepositoryMock.Object, CancellationToken.None);

        Assert.That(predicate, Is.Not.Null);
        Assert.That(predicate.Compile()(deleted), Is.False);
        Assert.That(result.Result, Is.TypeOf<Ok<ListTreatmentsResponse>>());
    }

    [Test]
    public async Task HandleAsync_Should_Filter_By_User()
    {
        var otherUsersTreatment = CreateTreatment();

        Expression<Func<Treatment, bool>> predicate = null;

        _treatmentsRepositoryMock
            .Setup(r => r.Find(It.IsAny<Expression<Func<Treatment, bool>>>(), It.IsAny<FindOptions>()))
            .Callback<Expression<Func<Treatment, bool>>, FindOptions>((expression, _) => predicate = expression)
            .Returns(new TestAsyncEnumerable<Treatment>(new[] { otherUsersTreatment }));

        _treatmentsRepositoryMock
            .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Treatment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var request = new ListTreatmentsRequest { Page = 0, PageSize = 10 };

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _currentUserMock.Object, _treatmentsRepositoryMock.Object, CancellationToken.None);

        Assert.That(predicate, Is.Not.Null);
        Assert.That(predicate.Compile()(otherUsersTreatment), Is.False);
        Assert.That(result.Result, Is.TypeOf<Ok<ListTreatmentsResponse>>());
    }

    [Test]
    public async Task HandleAsync_Should_Paginate_Treatments()
    {
        var treatments = new List<Treatment>
        {
            CreateTreatment(created: DateTimeOffset.UtcNow.AddHours(-2)),
            CreateTreatment(created: DateTimeOffset.UtcNow.AddHours(-1)),
        };

        _treatmentsRepositoryMock
            .Setup(r => r.Find(It.IsAny<Expression<Func<Treatment, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Treatment>(treatments));

        _treatmentsRepositoryMock
            .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Treatment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(treatments.Count);

        var request = new ListTreatmentsRequest { Page = 1, PageSize = 1 };

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _currentUserMock.Object, _treatmentsRepositoryMock.Object, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<Ok<ListTreatmentsResponse>>());
        var okResult = result.Result as Ok<ListTreatmentsResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult!.Value.Treatments.Count, Is.EqualTo(1));
            Assert.That(okResult.Value.NumberOfPages, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task HandleAsync_Should_Calculate_Number_Of_Pages()
    {
        var request = new ListTreatmentsRequest { Page = 0, PageSize = 3 };

        _treatmentsRepositoryMock
            .Setup(r => r.Find(It.IsAny<Expression<Func<Treatment, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Treatment>(Enumerable.Empty<Treatment>()));

        _treatmentsRepositoryMock
            .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Treatment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _currentUserMock.Object, _treatmentsRepositoryMock.Object, CancellationToken.None);

        var okResult = result.Result as Ok<ListTreatmentsResponse>;
        Assert.That(okResult!.Value.NumberOfPages, Is.EqualTo(3));
    }

    [Test]
    public async Task HandleAsync_Should_Sort_By_Requested_Field_Ascending()
    {
        var treatments = new List<Treatment>
        {
            CreateTreatment(calories: 500m),
            CreateTreatment(calories: 200m),
        };

        _treatmentsRepositoryMock
            .Setup(r => r.Find(It.IsAny<Expression<Func<Treatment, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Treatment>(treatments));

        _treatmentsRepositoryMock
            .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Treatment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(treatments.Count);

        var request = new ListTreatmentsRequest
        {
            Page = 0,
            PageSize = 10,
            SortBy = nameof(Treatment.Calories),
            SortDirection = SortDirection.Asc,
        };

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _currentUserMock.Object, _treatmentsRepositoryMock.Object, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<Ok<ListTreatmentsResponse>>());
        var okResult = result.Result as Ok<ListTreatmentsResponse>;
        Assert.That(okResult!.Value.Treatments.Select(t => t.Calories), Is.EqualTo(new[] { 200m, 500m }));
    }

    [Test]
    public async Task HandleAsync_Should_Sort_By_Requested_Field_Descending()
    {
        var treatments = new List<Treatment>
        {
            CreateTreatment(calories: 200m),
            CreateTreatment(calories: 500m),
        };

        _treatmentsRepositoryMock
            .Setup(r => r.Find(It.IsAny<Expression<Func<Treatment, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Treatment>(treatments));

        _treatmentsRepositoryMock
            .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Treatment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(treatments.Count);

        var request = new ListTreatmentsRequest
        {
            Page = 0,
            PageSize = 10,
            SortBy = nameof(Treatment.Calories),
            SortDirection = SortDirection.Desc,
        };

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _currentUserMock.Object, _treatmentsRepositoryMock.Object, CancellationToken.None);

        var okResult = result.Result as Ok<ListTreatmentsResponse>;
        Assert.That(okResult!.Value.Treatments.Select(t => t.Calories), Is.EqualTo(new[] { 500m, 200m }));
    }
}