using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Api.Endpoints.Treatments.DeleteTreatment;
using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data.Entities;
using CgmLink.Data.Enums;
using CgmLink.Data.Repository;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using NUnit.Framework;

namespace CgmLink.Api.Tests.Endpoints.Treatments;

[TestFixture]
public class DeleteTreatmentTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private Mock<IRepository<Treatment>> _treatmentsRepositoryMock;
    private Mock<IRepository<Injection>> _injectionsRepositoryMock;
    private Mock<ICurrentUser> _currentUserMock;

    [SetUp]
    public void SetUp()
    {
        _treatmentsRepositoryMock = new Mock<IRepository<Treatment>>();
        _injectionsRepositoryMock = new Mock<IRepository<Injection>>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(c => c.GetUserId()).Returns(_userId);

        _treatmentsRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<Treatment, bool>>>(), It.IsAny<FindOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Treatment?)null);

        _treatmentsRepositoryMock
            .Setup(r => r.DeleteAsync(It.IsAny<Treatment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _injectionsRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<Injection, bool>>>(), It.IsAny<FindOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Injection?)null);

        _injectionsRepositoryMock
            .Setup(r => r.DeleteAsync(It.IsAny<Injection>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private Injection CreateInjection(Guid id)
    {
        return new Injection
        {
            Id = id,
            UserId = _userId,
            InsulinId = Guid.NewGuid(),
            Insulin = new Insulin
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                Name = "Fiasp",
                Type = InsulinType.Bolus,
                Created = DateTimeOffset.UtcNow,
            },
            Units = 5m,
            Created = DateTimeOffset.UtcNow,
        };
    }

    private Treatment CreateTreatment(Guid id)
    {
        return new Treatment
        {
            Id = id,
            UserId = _userId,
            Calories = 0m,
            Carbs = 0m,
            Protein = 0m,
            Fat = 0m,
            Created = DateTimeOffset.UtcNow.AddDays(-2),
        };
    }

    private void SetupTreatment(Treatment treatment)
    {
        _treatmentsRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<Treatment, bool>>>(), It.IsAny<FindOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Treatment, bool>> predicate, FindOptions _, CancellationToken _) =>
                predicate.Compile()(treatment) ? treatment : null);
    }

    [Test]
    public async Task HandleAsync_Should_Return_NoContent_And_Delete_Treatment_And_Injection_When_Treatment_Has_Injection()
    {
        var treatmentId = Guid.NewGuid();
        var injectionId = Guid.NewGuid();
        var treatment = CreateTreatment(treatmentId);
        treatment.InjectionId = injectionId;
        var injection = CreateInjection(injectionId);
        SetupTreatment(treatment);

        _injectionsRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<Injection, bool>>>(), It.IsAny<FindOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(injection);

        var result = await Endpoint.HandleAsync(treatmentId, _currentUserMock.Object,
            _treatmentsRepositoryMock.Object, _injectionsRepositoryMock.Object, CancellationToken.None);

        _treatmentsRepositoryMock.Verify(r => r.DeleteAsync(It.Is<Treatment>(t => t.Id == treatmentId), It.IsAny<CancellationToken>()), Times.Once);
        _injectionsRepositoryMock.Verify(r => r.DeleteAsync(It.Is<Injection>(i => i.Id == injectionId), It.IsAny<CancellationToken>()), Times.Once);

        Assert.That(result.Result, Is.TypeOf<NoContent>());
    }

    [Test]
    public async Task HandleAsync_Should_Return_NoContent_And_Delete_Only_Treatment_When_Treatment_Has_No_Injection()
    {
        var treatmentId = Guid.NewGuid();
        var treatment = CreateTreatment(treatmentId);
        SetupTreatment(treatment);

        var result = await Endpoint.HandleAsync(treatmentId, _currentUserMock.Object,
            _treatmentsRepositoryMock.Object, _injectionsRepositoryMock.Object, CancellationToken.None);

        _treatmentsRepositoryMock.Verify(r => r.DeleteAsync(It.Is<Treatment>(t => t.Id == treatmentId), It.IsAny<CancellationToken>()), Times.Once);
        _injectionsRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Injection>(), It.IsAny<CancellationToken>()), Times.Never);

        Assert.That(result.Result, Is.TypeOf<NoContent>());
    }

    [Test]
    public async Task HandleAsync_Should_Return_NoContent_And_Delete_Treatment_When_Injection_Reference_Is_Missing()
    {
        var treatmentId = Guid.NewGuid();
        var injectionId = Guid.NewGuid();
        var treatment = CreateTreatment(treatmentId);
        treatment.InjectionId = injectionId;
        SetupTreatment(treatment);

        var result = await Endpoint.HandleAsync(treatmentId, _currentUserMock.Object,
            _treatmentsRepositoryMock.Object, _injectionsRepositoryMock.Object, CancellationToken.None);

        _treatmentsRepositoryMock.Verify(r => r.DeleteAsync(It.Is<Treatment>(t => t.Id == treatmentId), It.IsAny<CancellationToken>()), Times.Once);
        _injectionsRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Injection>(), It.IsAny<CancellationToken>()), Times.Never);

        Assert.That(result.Result, Is.TypeOf<NoContent>());
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Treatment_Not_Found()
    {
        var treatmentId = Guid.NewGuid();

        Assert.That(async () => await Endpoint.HandleAsync(treatmentId, _currentUserMock.Object,
                _treatmentsRepositoryMock.Object, _injectionsRepositoryMock.Object, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("TREATMENT_NOT_FOUND"));

        _treatmentsRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Treatment>(), It.IsAny<CancellationToken>()), Times.Never);
        _injectionsRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Injection>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Treatment_Not_Linked_To_User()
    {
        var treatmentId = Guid.NewGuid();
        var treatment = CreateTreatment(treatmentId);
        treatment.UserId = Guid.NewGuid();
        SetupTreatment(treatment);

        Assert.That(async () => await Endpoint.HandleAsync(treatmentId, _currentUserMock.Object,
                _treatmentsRepositoryMock.Object, _injectionsRepositoryMock.Object, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("TREATMENT_NOT_FOUND"));

        _treatmentsRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Treatment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Treatment_Is_Soft_Deleted()
    {
        var treatmentId = Guid.NewGuid();
        var treatment = CreateTreatment(treatmentId);
        treatment.Deleted = DateTimeOffset.UtcNow;
        SetupTreatment(treatment);

        Assert.That(async () => await Endpoint.HandleAsync(treatmentId, _currentUserMock.Object,
                _treatmentsRepositoryMock.Object, _injectionsRepositoryMock.Object, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("TREATMENT_NOT_FOUND"));

        _treatmentsRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Treatment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void HandleAsync_Should_Throw_UnauthorizedAccessException_When_User_Is_Not_Logged_In()
    {
        var treatmentId = Guid.NewGuid();

        _currentUserMock
            .Setup(c => c.GetUserId())
            .Throws<UnauthorizedAccessException>();

        Assert.That(async () => await Endpoint.HandleAsync(treatmentId, _currentUserMock.Object,
                _treatmentsRepositoryMock.Object, _injectionsRepositoryMock.Object, CancellationToken.None),
            Throws.InstanceOf<UnauthorizedAccessException>());
    }
}