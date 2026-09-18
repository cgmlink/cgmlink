using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Identity.Authentication;
using CgmLink.Identity.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using Moq;
using DataGlucoseProvider = CgmLink.Data.Enums.GlucoseProvider;
using Endpoint = CgmLink.Identity.Endpoints.Me.Endpoint;
using IdentityGlucoseProvider = CgmLink.Identity.Models.GlucoseProvider;

namespace CgmLink.Identity.Tests.Endpoints.Me;

[TestFixture]
internal sealed class EndpointTests
{
    private Mock<ICurrentUser> _currentUser;
    private Mock<IRepository<User>> _userRepository;

    [SetUp]
    public void SetUp()
    {
        _currentUser = new Mock<ICurrentUser>();
        _userRepository = new Mock<IRepository<User>>();
    }

    [Test]
    public async Task HandleAsync_WithPatient_ReturnsPatientDetails()
    {
        var user = new Patient
        {
            Id = Guid.NewGuid(),
            Email = "patient@example.com",
            PasswordHash = "password",
            IsVerified = true,
            GlucoseProvider = DataGlucoseProvider.LibreLink
        };
        _currentUser.Setup(currentUser => currentUser.GetUserId()).Returns(user.Id);
        _userRepository
            .Setup(repository => repository.FindOneAsync(It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<FindOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await Endpoint.HandleAsync(_currentUser.Object, _userRepository.Object,
            Options.Create(new IdentityOptions { RequireEmailVerification = true }), new DefaultHttpContext(),
            CancellationToken.None);

        var response = ((Ok<MeResponse>)result.Result).Value;
        Assert.Multiple(() =>
        {
            Assert.That(response.Id, Is.EqualTo(user.Id));
            Assert.That(response.Email, Is.EqualTo(user.Email));
            Assert.That(response.IsVerified, Is.True);
            Assert.That(response.UserType, Is.EqualTo(UserType.Patient));
            Assert.That(response.GlucoseProvider, Is.EqualTo(IdentityGlucoseProvider.LibreLink));
        });
    }

    [Test]
    public async Task HandleAsync_WithCareGiverAndVerificationNotRequired_ReturnsUnknownVerifiedUser()
    {
        var user = new CareGiver
        {
            Id = Guid.NewGuid(),
            Email = "caregiver@example.com",
            PasswordHash = "password",
            IsVerified = false
        };
        _currentUser.Setup(currentUser => currentUser.GetUserId()).Returns(user.Id);
        _userRepository
            .Setup(repository => repository.FindOneAsync(It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<FindOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await Endpoint.HandleAsync(_currentUser.Object, _userRepository.Object,
            Options.Create(new IdentityOptions { RequireEmailVerification = false }), new DefaultHttpContext(),
            CancellationToken.None);

        var response = ((Ok<MeResponse>)result.Result).Value;
        Assert.Multiple(() =>
        {
            Assert.That(response.IsVerified, Is.True);
            Assert.That(response.UserType, Is.EqualTo(UserType.Unknown));
            Assert.That(response.GlucoseProvider, Is.Null);
        });
    }

    [Test]
    public void HandleAsync_WithMissingUser_ThrowsUnauthorizedException()
    {
        _currentUser.Setup(currentUser => currentUser.GetUserId()).Returns(Guid.NewGuid());
        _userRepository
            .Setup(repository => repository.FindOneAsync(It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<FindOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User)null);

        var exception = Assert.ThrowsAsync<UnauthorizedException>(() => Endpoint.HandleAsync(_currentUser.Object,
            _userRepository.Object, Options.Create(new IdentityOptions()), new DefaultHttpContext(),
            CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Is.EqualTo("USER_NOT_LOGGED_IN"));
            Assert.That(exception.UnauthorizedSource, Is.EqualTo(UnauthorizedSource.CgmLink));
        });
    }
}
