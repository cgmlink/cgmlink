using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Identity.Models;
using CgmLink.Identity.Services;
using CgmLink.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace CgmLink.Identity.Tests.Services;

[TestFixture]
internal sealed class UserServiceTests
{
    private Mock<CgmLinkDbContext> _dbContext;
    private Mock<IRepository<User>> _userRepository;
    private Mock<IRepository<AlarmRule>> _alarmRepository;
    private Mock<ITokenService> _tokenService;
    private Mock<IMailService> _mailService;
    private Mock<ITemplateService> _templateService;
    private Mock<ILogger<UserService>> _logger;
    private IdentityOptions _options;
    private Mock<IOptions<IdentityOptions>> _identityOptions;
    private UserService _sut;

    [SetUp]
    public void Setup()
    {
        _dbContext = new Mock<CgmLinkDbContext>(new DbContextOptionsBuilder<CgmLinkDbContext>()
            .UseSqlServer("Server=unused;Database=unit-tests").Options)
        { CallBase = true };
        _dbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => _dbContext.Object.ChangeTracker.DetectChanges()).ReturnsAsync(1);
        _userRepository = new Mock<IRepository<User>>();
        _alarmRepository = new Mock<IRepository<AlarmRule>>();
        _tokenService = new Mock<ITokenService>();
        _mailService = new Mock<IMailService>();
        _templateService = new Mock<ITemplateService>();
        _logger = new Mock<ILogger<UserService>>();
        _identityOptions = new Mock<IOptions<IdentityOptions>>();
        _options = new IdentityOptions() { RequireEmailVerification = false, VerifyEmailBaseUri = "https://localhost/" };
        _identityOptions.Setup(x => x.Value).Returns(_options);

        _sut = new UserService(_userRepository.Object, _alarmRepository.Object, _tokenService.Object, _mailService.Object,
            _templateService.Object, _identityOptions.Object, _logger.Object, _dbContext.Object);
    }

    [TearDown]
    public void TearDown() => _dbContext.Object.Dispose();

    [Test]
    public void Constructor_Should_Throw_ArgumentNullExceptions()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => new UserService(null!, _alarmRepository.Object, _tokenService.Object, _mailService.Object, _templateService.Object,
                    _identityOptions.Object, _logger.Object, _dbContext.Object), Throws.ArgumentNullException);
            Assert.That(
                () => new UserService(_userRepository.Object!, null!, _tokenService.Object, _mailService.Object, _templateService.Object,
                    _identityOptions.Object, _logger.Object, _dbContext.Object), Throws.ArgumentNullException);
            Assert.That(
                () => new UserService(_userRepository.Object!, _alarmRepository.Object, null!, _mailService.Object, _templateService.Object,
                    _identityOptions.Object, _logger.Object, _dbContext.Object), Throws.ArgumentNullException);
            Assert.That(
                () => new UserService(_userRepository.Object!, _alarmRepository.Object, _tokenService.Object, _mailService.Object, null!,
                    _identityOptions.Object, _logger.Object, _dbContext.Object), Throws.ArgumentNullException);
            Assert.That(
                () => new UserService(_userRepository.Object!, _alarmRepository.Object, _tokenService.Object, _mailService.Object,
                    _templateService.Object, null!, _logger.Object, _dbContext.Object), Throws.ArgumentNullException);
            Assert.That(
                () => new UserService(_userRepository.Object!, _alarmRepository.Object, _tokenService.Object, _mailService.Object,
                    _templateService.Object, _identityOptions.Object, null!, _dbContext.Object), Throws.ArgumentNullException);
            Assert.That(
                () => new UserService(_userRepository.Object, _alarmRepository.Object, _tokenService.Object, _mailService.Object,
                    _templateService.Object, _identityOptions.Object, _logger.Object, null!), Throws.ArgumentNullException);
        });
    }

    [Test]
    public async Task LoginAsync_WithValidPatientCredentials_ReturnsLoginResponse()
    {
        var request = new LoginRequest { Email = "test@example.com", Password = "password" };
        var user = new Patient
        { Email = "test@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"), PatientId = "PatientId" };

        _userRepository.Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<FindOptions>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(() => TrackUser(user));

        _tokenService.Setup(t => t.GenerateRefreshToken(It.IsAny<string>())).Returns(ActiveToken("refresh_token"));

        var result = await _sut.LoginAsync(request, "127.0.0.1");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Token, Is.Not.Empty);
            Assert.That(result.PatientId, Is.EqualTo(user.PatientId));
        });
    }

    [Test]
    public async Task LoginAsync_Returns_Patient_Targets_When_Patient()
    {
        var request = new LoginRequest { Email = "test@example.com", Password = "password" };
        var user = new Patient
        { Email = "test@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"), AlarmRules = [new AlarmRule() { PatientId = Guid.NewGuid(), TargetDirection = (Data.Enums.AlarmTargetDirection)AlarmTargetDirection.GreaterThan, TargetValue = 10 }], TargetHigh = 10, TargetLow = 4 };
        var alarmRule = new AlarmRule { Id = Guid.NewGuid(), PatientId = user.Id, TargetDirection = (Data.Enums.AlarmTargetDirection)AlarmTargetDirection.GreaterThan, TargetValue = 10 };

        _userRepository.Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<FindOptions>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(() => TrackUser(user));
        _alarmRepository.Setup(r => r.Find(It.IsAny<Expression<Func<AlarmRule, bool>>>(), It.IsAny<FindOptions>())).Returns(new List<AlarmRule>() { alarmRule }.AsQueryable());

        _tokenService.Setup(t => t.GenerateRefreshToken(It.IsAny<string>())).Returns(ActiveToken("refresh_token"));

        var result = await _sut.LoginAsync(request, "127.0.0.1");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Token, Is.Not.Empty);
            Assert.That(result.TargetHigh, Is.EqualTo(user.TargetHigh));
            Assert.That(result.TargetLow, Is.EqualTo(user.TargetLow));
            Assert.That(result.AlarmRules.First().Id, Is.EqualTo(alarmRule.Id));
        });
    }

    [Test]
    public async Task LoginAsync_Does_Not_Return_Patient_Targets_When_CareGiver()
    {
        var request = new LoginRequest { Email = "test@example.com", Password = "password" };
        var user = new CareGiver
        { Email = "test@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") };

        _userRepository.Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<FindOptions>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(() => TrackUser(user));

        _tokenService.Setup(t => t.GenerateRefreshToken(It.IsAny<string>())).Returns(ActiveToken("refresh_token"));

        var result = await _sut.LoginAsync(request, "127.0.0.1");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Token, Is.Not.Empty);
            Assert.That(result.TargetHigh, Is.Null);
            Assert.That(result.TargetLow, Is.Null);
            Assert.That(result.AlarmRules, Is.Null);
        });
    }

    [Test]
    public async Task LoginAsync_WithValidCareGiverCredentials_ReturnsLoginResponse()
    {
        _tokenService.Setup(t => t.GenerateRefreshToken(It.IsAny<string>())).Returns(ActiveToken("refresh_token"));

        var request = new LoginRequest { Email = "test@example.com", Password = "password" };
        var user = new CareGiver
        {
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            Created = DateTimeOffset.UtcNow,
            RefreshTokens =
            []
        };
        _userRepository.Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<FindOptions>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(() => TrackUser(user));

        _tokenService.Setup(t => t.GenerateRefreshToken(It.IsAny<string>())).Returns(ActiveToken("refresh_token"));

        var result = await _sut.LoginAsync(request, "127.0.0.1");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Token, Is.Not.Empty);
            Assert.That(result.RefreshToken, Is.EqualTo("refresh_token"));
        });
    }

    [Test]
    public void LoginAsync_WithValidPatientCredentials_Unverified_Throws_Forbidden()
    {
        _options.RequireEmailVerification = true;
        var request = new LoginRequest { Email = "test@example.com", Password = "password" };
        var user = new Patient
        { Email = "test@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") };

        _userRepository.Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<FindOptions>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(() => TrackUser(user));

        Assert.That(() => _sut.LoginAsync(request, "127.0.0.1"), Throws.TypeOf<ForbiddenException>());
    }

    [Test]
    public void LoginAsync_WithValidCareGiverCredentials_Unverified_Throws_Forbidden()
    {
        _options.RequireEmailVerification = true;
        var request = new LoginRequest { Email = "test@example.com", Password = "password" };
        var user = new CareGiver
        { Email = "test@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") };

        _userRepository.Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<FindOptions>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(() => TrackUser(user));

        Assert.That(() => _sut.LoginAsync(request, "127.0.0.1"), Throws.TypeOf<ForbiddenException>());
    }

    [Test]
    public void LoginAsync_WithInvalidPatientCredentials_ThrowsUnauthorizedException()
    {
        var request = new LoginRequest { Email = "test@example.com", Password = "wrongpassword" };
        var user = new Patient
        { Email = "test@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") };
        _userRepository.Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<FindOptions>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(() => TrackUser(user));

        Assert.That(() => _sut.LoginAsync(request, "127.0.0.1"), Throws.TypeOf<UnauthorizedException>());
    }

    [Test]
    public void LoginAsync_WithInvalidCareGiverCredentials_ThrowsUnauthorizedException()
    {
        var request = new LoginRequest { Email = "test@example.com", Password = "wrongpassword" };
        var user = new CareGiver
        { Email = "test@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") };
        _userRepository.Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<FindOptions>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(() => TrackUser(user));

        Assert.That(() => _sut.LoginAsync(request, "127.0.0.1"), Throws.TypeOf<UnauthorizedException>());
    }

    [Test]
    public async Task RegisterAsync_WithNewPatient_ReturnsRegisterResponse()
    {
        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "password",
            ConfirmPassword = "password",
            AcceptedTerms = true,
            FirstName = "first name",
            LastName = "last name",
        };

        var result = await _sut.RegisterAsync(request, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Email, Is.EqualTo(request.Email));
        });
    }

    [Test]
    public async Task RegisterAsync_WithNewCareGiver_ReturnsRegisterResponse()
    {
        var patient = new Patient()
        { Email = "test@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") };

        _userRepository.Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<FindOptions>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(patient);

        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "password",
            ConfirmPassword = "password",
            AcceptedTerms = true,
            PatientId = patient.Id
        };

        var result = await _sut.RegisterAsync(request, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Email, Is.EqualTo(request.Email));
        });
    }

    [Test]
    public async Task RegisterAsync_Patient_RequiresEmailVerification()
    {
        _options.RequireEmailVerification = true;
        var patient = new Patient()
        { Email = "test@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") };

        _userRepository.Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<FindOptions>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(patient);

        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "password",
            ConfirmPassword = "password",
            AcceptedTerms = true,
            PatientId = patient.Id
        };

        _ = await _sut.RegisterAsync(request, CancellationToken.None);

        _mailService.Verify(
            m => m.SendAsync(It.Is<MailRequest>(x => x.To.SequenceEqual(new[] { request.Email })),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RegisterAsync_CareGiver_RequiresEmailVerification()
    {
        _options.RequireEmailVerification = true;

        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "password",
            ConfirmPassword = "password",
            AcceptedTerms = true,
        };

        _ = await _sut.RegisterAsync(request, CancellationToken.None);

        _mailService.Verify(
            m => m.SendAsync(It.Is<MailRequest>(x => x.To.SequenceEqual(new[] { request.Email })),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void RegisterAsync_WithExistingUser_ThrowsConflictException()
    {
        var request = new RegisterRequest
        {
            Email = "existinguser@example.com",
            Password = "password",
            ConfirmPassword = "password",
            AcceptedTerms = true
        };
        _userRepository.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Assert.That(() => _sut.RegisterAsync(request, CancellationToken.None),
            Throws.InstanceOf<ConflictException>());
    }

    [Test]
    public void RegisterAsync_WithNonExistentPatientId_ThrowsNotFoundException()
    {
        var request = new RegisterRequest
        {
            Email = "caregiver@example.com",
            Password = "password",
            ConfirmPassword = "password",
            AcceptedTerms = true,
            PatientId = Guid.NewGuid()
        };

        Assert.That(() => _sut.RegisterAsync(request, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>());
    }

    [Test]
    public void VerifyEmailAsync_Throws_UnauthorizedException()
    {
        var request = new VerifyEmailRequest
        {
            Token = "token"
        };

        _userRepository
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<FindOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User)null);

        Assert.That(() => _sut.VerifyEmailAsync(request), Throws.TypeOf<UnauthorizedException>());
    }

    [Test]
    public async Task VerifyEmailAsync_Updates_User()
    {
        var user = new Patient
        {
            Email = "user@nomail.com",
            PasswordHash = "password",
            EmailVerificationToken = "token",
        };
        var request = new VerifyEmailRequest
        {
            Token = "token"
        };

        _userRepository
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<FindOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => TrackUser(user));

        await _sut.VerifyEmailAsync(request, CancellationToken.None);

        _userRepository.Verify(
            r => r.UpdateAsync(It.Is<User>(u => u.EmailVerificationToken == null && u.IsVerified),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RefreshTokenAsync_With_Valid_Token_Returns_New_Token_Response()
    {
        var user = new Patient
        {
            Email = "existinguser@example.com",
            PasswordHash = "password",
            RefreshTokens =
            [
                new RefreshToken
                    { Token = "valid-token", Expires = DateTime.UtcNow.AddMinutes(5), CreatedByIp = "127.0.0.1" }
            ]
        };
        _userRepository.Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<FindOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => TrackUser(user));

        var newRefreshToken = new RefreshToken
        {
            Token = "new-refresh-token",
            Expires = DateTime.UtcNow.AddMinutes(5),
            CreatedByIp = "127.0.0.1",
        };
        _tokenService.Setup(t => t.GenerateRefreshToken("127.0.0.1")).Returns(newRefreshToken);
        var newJwtToken = "new-token";
        _tokenService.Setup(t => t.GenerateJwtToken(user)).Returns(newJwtToken);

        var result = await _sut.RefreshTokenAsync("valid-token", "127.0.0.1", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Token, Is.EqualTo(newJwtToken));
            Assert.That(result.RefreshToken, Is.EqualTo(newRefreshToken.Token));
        });
    }

    [Test]
    public async Task LoginAsync_Preserves_Existing_Active_Token_While_Removing_Expired_History()
    {
        var existing = ActiveToken("other-device");
        var expired = ActiveToken("expired-history");
        expired.Created = DateTimeOffset.UtcNow.AddDays(-60);
        expired.Expires = DateTimeOffset.UtcNow.AddDays(-31);
        var user = StubUser(existing, expired);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("password");
        _tokenService.Setup(x => x.GenerateRefreshToken(It.IsAny<string>())).Returns(ActiveToken("new-device"));

        await _sut.LoginAsync(new LoginRequest { Email = user.Email, Password = "password" }, "127.0.0.1");

        Assert.That(existing.IsActive, Is.True);
        Assert.That(user.RefreshTokens.Select(t => t.Token), Is.EquivalentTo(new[] { "other-device", "new-device" }));
    }

    [Test]
    public async Task RefreshTokenAsync_Rotates_Only_The_Presented_Token()
    {
        var original = ActiveToken("device-a");
        var otherDevice = ActiveToken("device-b");
        var user = StubUser(original, otherDevice);
        var replacement = ActiveToken("replacement");
        _tokenService.Setup(x => x.GenerateRefreshToken(It.IsAny<string>())).Returns(replacement);

        await _sut.RefreshTokenAsync(original.Token, "127.0.0.1");

        Assert.That(original.IsRevoked, Is.True);
        Assert.That(original.ReplacedByToken, Is.EqualTo(replacement.Token));
        Assert.That(user.RefreshTokens, Does.Contain(replacement));
        Assert.That(otherDevice.IsActive, Is.True);
        Assert.That(otherDevice.ReplacedByToken, Is.Null);
    }

    [Test]
    public void RefreshTokenAsync_Rejects_Concurrency_Conflict_Without_Issuing_Access_Token()
    {
        StubUser(ActiveToken("device-a"));
        _tokenService.Setup(x => x.GenerateRefreshToken(It.IsAny<string>())).Returns(ActiveToken("replacement"));
        _dbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Assert.ThrowsAsync<UnauthorizedException>(() => _sut.RefreshTokenAsync("device-a", "127.0.0.1"));

        _tokenService.Verify(x => x.GenerateJwtToken(It.IsAny<User>()), Times.Never);
        Assert.That(_dbContext.Object.ChangeTracker.HasChanges(), Is.False, "The losing replacement must not remain pending.");
    }

    [Test]
    public async Task RefreshTokenAsync_Succeeds_When_Other_Request_Already_Removed_Expired_History()
    {
        var expired = ActiveToken("expired-history");
        expired.Created = DateTimeOffset.UtcNow.AddDays(-60);
        expired.Expires = DateTimeOffset.UtcNow.AddDays(-31);
        StubUser(ActiveToken("device-a"), expired);
        var entry = _dbContext.Object.Entry(expired);
        var affectedEntry = new Mock<IUpdateEntry>();
        affectedEntry.Setup(x => x.ToEntityEntry()).Returns(entry);
        var conflict = new DbUpdateConcurrencyException("History already deleted", new[] { affectedEntry.Object });
        _tokenService.Setup(x => x.GenerateRefreshToken(It.IsAny<string>())).Returns(ActiveToken("replacement"));
        _dbContext.SetupSequence(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                _dbContext.Object.ChangeTracker.DetectChanges();
                return Task.FromException<int>(conflict);
            })
            .ReturnsAsync(1);

        var response = await _sut.RefreshTokenAsync("device-a", "127.0.0.1");

        Assert.That(response.RefreshToken, Is.EqualTo("replacement"));
    }

    private User TrackUser(User user)
    {
        _dbContext.Object.Attach(user);
        return user;
    }

    private User StubUser(params RefreshToken[] tokens)
    {
        var user = new Patient { Email = "user@example.com", PasswordHash = "unused", RefreshTokens = tokens.ToList() };
        _dbContext.Object.Attach(user);
        _userRepository.Setup(x => x.FindOneAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<FindOptions>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(user);
        return user;
    }

    private static RefreshToken ActiveToken(string token) => new()
    {
        Token = token,
        CreatedByIp = "127.0.0.1",
        Created = DateTimeOffset.UtcNow,
        Expires = DateTimeOffset.UtcNow.AddDays(30),
    };

    [Test]
    public void RefreshTokenAsync_With_Null_Token_Throws_UnauthorizedException()
    {
        Assert.That(() => _sut.RefreshTokenAsync(null, "127.0.0.1", CancellationToken.None),
            Throws.TypeOf<UnauthorizedException>());
    }

    [Test]
    public void RefreshTokenAsync_With_Invalid_Token_Throws_UnauthorizedException()
    {
        _userRepository.Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<FindOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User)null);

        Assert.That(() => _sut.RefreshTokenAsync("invalid-token", "127.0.0.1", CancellationToken.None),
            Throws.TypeOf<UnauthorizedException>());
    }

    [Test]
    public void RefreshTokenAsync_With_Revoked_Token_Throws_UnauthorizedException()
    {
        var user = new Patient
        {
            Email = "test@nomail.com",
            PasswordHash = "password",
            RefreshTokens = [new RefreshToken { Token = "revoked-token", CreatedByIp = "127.0.0.1" }]
        };
        _userRepository.Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<FindOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => TrackUser(user));

        Assert.That(() => _sut.RefreshTokenAsync("revoked-token", "127.0.0.1", CancellationToken.None),
            Throws.TypeOf<UnauthorizedException>());
    }

    [Test]
    public void RefreshTokenAsync_With_Expired_Token_Throws_UnauthorizedException()
    {
        var user = new Patient
        {
            Email = "test@nomail.com",
            PasswordHash = "password",
            RefreshTokens =
            [
                new RefreshToken
                    { Token = "expired-token", Expires = DateTime.UtcNow.AddMinutes(-5), CreatedByIp = "127.0.0.1" }
            ]
        };
        _userRepository.Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<FindOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => TrackUser(user));

        Assert.That(() => _sut.RefreshTokenAsync("expired-token", "127.0.0.1", CancellationToken.None),
            Throws.TypeOf<UnauthorizedException>());
    }

    [Test]
    public void RefreshTokenAsync_When_Refresh_Token_Is_Revoked_Revokes_Recursively()
    {
        var revokedToken = new RefreshToken
        {
            Token = "revoked-token",
            Revoked = DateTimeOffset.Now.AddMinutes(-1),
            ReplacedByToken = "child-token",
            CreatedByIp = "127.0.0.1",
        };

        var childToken = new RefreshToken
        {
            Token = "child-token",
            Expires = DateTimeOffset.Now.AddMinutes(1),
            CreatedByIp = "127.0.0.1",
        };

        var user = new Patient
        {
            Email = "test@nomail.com",
            PasswordHash = "password",
            RefreshTokens = [revokedToken, childToken, ActiveToken("other-device")]
        };

        _userRepository.Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<FindOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => TrackUser(user));

        Assert.Multiple(() =>
        {
            Assert.That(async () => await _sut.RefreshTokenAsync("revoked-token", "127.0.0.1", CancellationToken.None),
                Throws.TypeOf<UnauthorizedException>());

            Assert.That(revokedToken.IsRevoked, Is.True);
            Assert.That(childToken.IsRevoked, Is.True);
            Assert.That(user.RefreshTokens.Single(t => t.Token == "other-device").IsActive, Is.True);
            Assert.That(childToken.RevokedReason, Does.Not.Contain(revokedToken.Token));
        });

        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Refresh token replay detected")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task Revoke_Token_Async_With_Valid_Token_Revokes_Token_Successfully()
    {
        var token = "valid-token";
        var ipAddress = "127.0.0.1";
        var refreshToken = new RefreshToken { Token = token, CreatedByIp = "127.0.0.1", Expires = DateTimeOffset.Now.AddMinutes(5) };
        var user = new Patient { Email = "test@nomail.com", PasswordHash = "password", RefreshTokens = [refreshToken, ActiveToken("other-device")] };

        _userRepository.Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<FindOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => TrackUser(user));

        await _sut.RevokeTokenAsync(token, ipAddress, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(refreshToken.IsActive, Is.False);
            Assert.That(refreshToken.Revoked, Is.Not.Null);
            Assert.That(refreshToken.RevokedByIp, Is.EqualTo(ipAddress));
            Assert.That(refreshToken.RevokedReason, Is.EqualTo("Revoked without replacement"));
            Assert.That(user.RefreshTokens.Single(t => t.Token == "other-device").IsActive, Is.True);
        });

        _dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void Revoke_Token_Async_With_Invalid_Token_Throws_UnauthorizedException()
    {
        var token = "invalid-token";

        _userRepository.Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<FindOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User)null);

        Assert.That(async () => await _sut.RevokeTokenAsync(token, "127.0.0.1", CancellationToken.None),
            Throws.TypeOf<UnauthorizedException>());
    }

    [Test]
    public void Revoke_Token_Async_With_Inactive_Token_Throws_UnauthorizedException()
    {
        var token = "inactive-token";
        var refreshToken = new RefreshToken { Token = token, CreatedByIp = "127.0.0.1", Expires = DateTimeOffset.Now.AddMinutes(-5) };
        var user = new Patient { Email = "test@nomail.com", PasswordHash = "password", RefreshTokens = [refreshToken] };

        _userRepository.Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<FindOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => TrackUser(user));

        Assert.That(async () => await _sut.RevokeTokenAsync(token, "127.0.0.1", CancellationToken.None),
            Throws.TypeOf<UnauthorizedException>());
    }
}
