using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Identity.Models;
using CgmLink.Identity.Templates;
using CgmLink.Mail;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static BCrypt.Net.BCrypt;

namespace CgmLink.Identity.Services;

public sealed class UserService : IUserService
{
    private readonly CgmLinkDbContext _dbContext;
    private readonly IRepository<User> _repository;
    private readonly IRepository<AlarmRule> _alarmRepository;
    private readonly ITokenService _tokenService;
    private readonly IMailService _mailService;
    private readonly ITemplateService _templateService;
    private readonly IdentityOptions _options;
    private readonly ILogger<UserService> _logger;

    public UserService(IRepository<User> repository, IRepository<AlarmRule> alarmRepository, ITokenService tokenService,
        IMailService mailService,
        ITemplateService templateService, IOptions<IdentityOptions> options, ILogger<UserService> logger, CgmLinkDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _alarmRepository = alarmRepository ?? throw new ArgumentNullException(nameof(alarmRepository));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _mailService = mailService ?? throw new ArgumentNullException(nameof(mailService));
        _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var user = await _repository.FindOneAsync(u => u.Email == request.Email,
            new FindOptions { IsAsNoTracking = false, IsIgnoreAutoIncludes = true }, cancellationToken);

        if (user is null || !Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("EMAIL_OR_PASSWORD_INCORRECT", UnauthorizedSource.CgmLink);
        }

        if (_options.RequireEmailVerification && !user.IsVerified)
        {
            throw new ForbiddenException("EMAIL_NOT_VERIFIED");
        }
        
        List<AlarmRule>? alarmRules = null;
        if (user is Patient)
        {
            alarmRules = _alarmRepository.Find(x => x.PatientId == user.Id,
                    new FindOptions { IsAsNoTracking = true, IsIgnoreAutoIncludes = false })
                .Select(alarm => new AlarmRule
                {
                    Id = alarm.Id,
                    PatientId = alarm.PatientId,
                    TargetValue = alarm.TargetValue,
                    TargetDirection = alarm.TargetDirection
                })
                .ToList();
        }

        var token = _tokenService.GenerateJwtToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken(ipAddress);
        AddRefreshToken(user, refreshToken);

        RemoveOldRefreshTokens(user);

        await SaveRefreshTokenChangesAsync(user, refreshToken, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Refresh token issued for user {UserId}, token {TokenId}", user.Id, refreshToken.Id);

        if (user is Patient)
        {
            var patient = (Patient)user;
            return new LoginResponse
            {
                Token = token,
                UserId = user.Id,
                Email = user.Email,
                IsVerified = user.IsVerified,
                GlucoseProvider = (GlucoseProvider?)patient.GlucoseProvider,
                TargetLow = patient.TargetLow,
                TargetHigh = patient.TargetHigh,
                AlarmRules = alarmRules is not null
                    ? alarmRules.Select(r => new LoginAlarmRuleResponse
                    {
                        Id = r.Id,
                        TargetDirection = (AlarmTargetDirection)r.TargetDirection,
                        TargetValue = r.TargetValue,
                    }).ToList()
                    : null,
                RefreshToken = refreshToken.Token,
                PatientId = patient.PatientId,
            };
        }

        return new LoginResponse
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            IsVerified = user.IsVerified,
            GlucoseProvider = null,
            TargetLow = null,
            TargetHigh = null,
            AlarmRules = null,
            RefreshToken = refreshToken.Token,
            PatientId = null,
        };
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        if (await _repository.AnyAsync(x => x.Email == request.Email, cancellationToken).ConfigureAwait(false))
        {
            throw new ConflictException("USER_ALREADY_EXISTS");
        }

        User user;
        if (request.PatientId is null)
        {
            var patient = new Patient
            {
                Email = request.Email,
                PasswordHash = HashPassword(request.Password),
                AcceptedTerms = request.AcceptedTerms,
                EmailVerificationToken = _options.RequireEmailVerification
                    ? await GenerateVerificationTokenAsync(cancellationToken).ConfigureAwait(false)
                    : null,
                IsVerified = !_options.RequireEmailVerification,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Settings = new UserSettings(),
            };
            user = patient;

            await _repository.AddAsync(patient, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var patient = await _repository.FindOneAsync(x => x.Id == request.PatientId.Value,
                    new FindOptions { IsAsNoTracking = true, IsIgnoreAutoIncludes = true }, cancellationToken)
                .ConfigureAwait(false);

            if (patient is not Patient patientEntity)
            {
                throw new NotFoundException("PATIENT_NOT_FOUND");
            }

            var careGiver = new CareGiver
            {
                Email = request.Email,
                PasswordHash = HashPassword(request.Password),
                AcceptedTerms = request.AcceptedTerms,
                Patients = [patientEntity],
                EmailVerificationToken = _options.RequireEmailVerification
                    ? await GenerateVerificationTokenAsync(cancellationToken).ConfigureAwait(false)
                    : null,
                IsVerified = !_options.RequireEmailVerification,
                FirstName = request.FirstName,
                LastName = request.LastName,
            };
            user = careGiver;

            await _repository.AddAsync(careGiver, cancellationToken).ConfigureAwait(false);
        }

        if (_options.RequireEmailVerification)
        {
            await SendVerificationEmailAsync(user.Email, user.EmailVerificationToken!, cancellationToken)
                .ConfigureAwait(false);
        }

        return new RegisterResponse
        {
            Id = user.Id,
            Email = user.Email,
            Created = user.Created,
            Updated = user.Updated,
            AcceptedTerms = user.AcceptedTerms,
            EmailVerified = !_options.RequireEmailVerification,
        };
    }

    public async Task SendVerificationEmailAsync(string email, string verificationToken,
        CancellationToken cancellationToken = default)
    {
        var confirmEmailModel = new EmailConfirmation()
        {
            Email = email,
            Url = GetEmailVerificationUrl(verificationToken),
        };

        var message = new MailRequest()
        {
            To = [email],
            Subject = "Verify your email",
            Body = await _templateService
                .RenderTemplateAsync("EmailConfirmation", confirmEmailModel, cancellationToken)
                .ConfigureAwait(false),
        };
        await _mailService.SendAsync(message, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TokenResponse> RefreshTokenAsync(string? token, string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var user = await FindByRefreshTokenAsync(token, cancellationToken).ConfigureAwait(false);
        var refreshToken = user.RefreshTokens.SingleOrDefault(x => x.Token == token);

        if (refreshToken?.IsRevoked ?? false)
        {
            _logger.LogWarning("Refresh token replay detected for user {UserId}, token {TokenId} from {IpAddress}",
                user.Id, refreshToken.Id, ipAddress);
            RevokeRefreshTokensRecursively(refreshToken, user, ipAddress,
                $"Attempted use of revoked ancestor token record: {refreshToken.Id}");
            await SaveRefreshTokenChangesAsync(user, refreshToken, cancellationToken).ConfigureAwait(false);
        }

        if (!(refreshToken?.IsActive ?? false))
        {
            throw new UnauthorizedException("REFRESH_TOKEN_INCORRECT", UnauthorizedSource.CgmLink);
        }

        var newRefreshToken = _tokenService.GenerateRefreshToken(ipAddress);
        RevokeRefreshToken(refreshToken, ipAddress, "Replaced by new token", newRefreshToken.Token);
        AddRefreshToken(user, newRefreshToken);

        RemoveOldRefreshTokens(user);

        await SaveRefreshTokenChangesAsync(user, refreshToken, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Refresh token rotated for user {UserId}, token {TokenId}, replacement {ReplacementTokenId}",
            user.Id, refreshToken.Id, newRefreshToken.Id);

        var jwtToken = _tokenService.GenerateJwtToken(user);
        var response = new TokenResponse()
        {
            Token = jwtToken,
            RefreshToken = newRefreshToken.Token,
        };

        return response;
    }

    public async Task<User> FindByRefreshTokenAsync(string? token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new UnauthorizedException("INVALID_TOKEN", UnauthorizedSource.CgmLink);
        }

        var user = await _repository.FindOneAsync(u => u.RefreshTokens.Any(t => t.Token == token),
                new FindOptions { IsAsNoTracking = false }, cancellationToken)
            .ConfigureAwait(false);
        if (user is null)
        {
            throw new UnauthorizedException("INVALID_TOKEN", UnauthorizedSource.CgmLink);
        }

        return user;
    }

    public async Task RevokeTokenAsync(string token, string ipAddress, CancellationToken cancellationToken = default)
    {
        var user = await FindByRefreshTokenAsync(token, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            throw new UnauthorizedException("INVALID_TOKEN", UnauthorizedSource.CgmLink);
        }

        var refreshToken = user.RefreshTokens.Single(x => x.Token == token);

        if (!refreshToken.IsActive)
        {
            throw new UnauthorizedException("INVALID_TOKEN", UnauthorizedSource.CgmLink);
        }

        RevokeRefreshToken(refreshToken, ipAddress, "Revoked without replacement");
        await SaveRefreshTokenChangesAsync(user, refreshToken, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Refresh token revoked for user {UserId}, token {TokenId}", user.Id, refreshToken.Id);
    }

    private void AddRefreshToken(User user, RefreshToken token)
    {
        user.RefreshTokens.Add(token);
        _dbContext.Entry(token).State = EntityState.Added;
    }

    private async Task SaveRefreshTokenChangesAsync(User user, RefreshToken token, CancellationToken cancellationToken)
    {
        while (true)
        {
            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (DbUpdateConcurrencyException exception) when (exception.Entries.Count > 0 &&
                exception.Entries.All(entry => entry.Entity is RefreshToken && entry.State == EntityState.Deleted))
            {
                foreach (var entry in exception.Entries)
                {
                    entry.State = EntityState.Detached;
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                _dbContext.ChangeTracker.Clear();
                _logger.LogWarning("Refresh token concurrency conflict for user {UserId}, token {TokenId}", user.Id, token.Id);
                throw new UnauthorizedException("REFRESH_TOKEN_INCORRECT", UnauthorizedSource.CgmLink);
            }
        }
    }

    private static void RevokeRefreshTokensRecursively(RefreshToken refreshToken, User user, string ipAddress,
        string reason)
    {
        if (string.IsNullOrEmpty(refreshToken.ReplacedByToken))
        {
            return;
        }

        var child = user.RefreshTokens.SingleOrDefault(x => x.Token == refreshToken.ReplacedByToken);
        if (child is null)
        {
            return;
        }

        if (child.IsActive)
        {
            RevokeRefreshToken(child, ipAddress, reason);
        }
        else
        {
            RevokeRefreshTokensRecursively(child, user, ipAddress, reason);
        }
    }

    private static void RevokeRefreshToken(RefreshToken refreshToken, string ipAddress, string? reason = null,
        string? replacedByToken = null)
    {
        refreshToken.Revoked = DateTime.UtcNow;
        refreshToken.RevokedByIp = ipAddress;
        refreshToken.RevokedReason = reason;
        refreshToken.ReplacedByToken = replacedByToken;
    }

    private async Task<string> GenerateVerificationTokenAsync(CancellationToken cancellationToken)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));

        var tokenIsUnique = !await _repository.AnyAsync(x => x.EmailVerificationToken == token, cancellationToken)
            .ConfigureAwait(false);
        if (!tokenIsUnique)
        {
            return await GenerateVerificationTokenAsync(cancellationToken).ConfigureAwait(false);
        }

        return token;
    }

    private string GetEmailVerificationUrl(string verificationToken)
    {
        const string route = "api/v1/identity/verify-email";
        var endpointUri =
            new Uri(string.Concat(
                $"{_options.VerifyEmailBaseUri?.TrimEnd('/') ?? throw new InvalidOperationException("VerifyEmailBaseUri is not configured.")}/",
                route));
        return QueryHelpers.AddQueryString(endpointUri.ToString(), "token", verificationToken);
    }

    public async Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _repository.FindOneAsync(x => x.EmailVerificationToken == request.Token,
                new FindOptions() { IsAsNoTracking = true, IsIgnoreAutoIncludes = true }, cancellationToken)
            .ConfigureAwait(false);
        if (user is null)
        {
            throw new UnauthorizedException("EMAIL_VERIFICATION_TOKEN_INVALID", UnauthorizedSource.CgmLink);
        }

        user.EmailVerificationToken = null;
        user.IsVerified = true;

        await _repository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
    }

    private void RemoveOldRefreshTokens(User user)
    {
        user.RefreshTokens.RemoveAll(x =>
            !x.IsActive && x.Created.AddDays(_options.RefreshTokenExpirationInDays) <= DateTime.UtcNow);
    }
}