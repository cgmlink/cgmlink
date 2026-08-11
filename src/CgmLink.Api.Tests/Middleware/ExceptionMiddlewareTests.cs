using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CgmLink.Api.Middleware;
using CgmLink.AspNetCore.Exceptions;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace CgmLink.Api.Tests.Middleware;

[TestFixture]
internal sealed class ExceptionMiddlewareTests
{
    private DefaultHttpContext _httpContext;
    private Mock<ICurrentUser> _currentUser;
    private Mock<ILogger<ExceptionMiddleware>> _logger;

    private ExceptionMiddleware _sut;

    [SetUp]
    public void SetUp()
    {
        _httpContext = new DefaultHttpContext();
        _httpContext.Response.Body = new MemoryStream();
        _currentUser = new Mock<ICurrentUser>();
        _logger = new Mock<ILogger<ExceptionMiddleware>>();

        _sut = new ExceptionMiddleware(_currentUser.Object, _logger.Object);
    }

    [Test]
    public async Task InvokeAsync_WithNoException_ReturnsOkStatusCode()
    {
        var next = new RequestDelegate(_ => Task.CompletedTask);

        await _sut.InvokeAsync(_httpContext, next);

        Assert.That(_httpContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
    }

    [Test]
    public async Task InvokeAsync_WithConflictException_ReturnsConflictStatusCode()
    {
        var next = new RequestDelegate(_ => throw new ConflictException("Conflict occurred"));

        await _sut.InvokeAsync(_httpContext, next);

        Assert.That(_httpContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status409Conflict));
    }

    [Test]
    public async Task InvokeAsync_WithNotFoundException_ReturnsNotFoundStatusCode()
    {
        var next = new RequestDelegate(_ => throw new NotFoundException("Not found"));

        await _sut.InvokeAsync(_httpContext, next);

        Assert.That(_httpContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
    }

    [Test]
    public async Task InvokeAsync_WithUnauthorizedException_ReturnsUnauthorizedStatusCode()
    {
        var next = new RequestDelegate(_ => throw new UnauthorizedException("Unauthorized", UnauthorizedSource.CgmLink));

        await _sut.InvokeAsync(_httpContext, next);

        Assert.That(_httpContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
    }

    [Test]
    public async Task InvokeAsync_WithForbiddenException_ReturnsForbiddenStatusCode()
    {
        var next = new RequestDelegate(_ => throw new ForbiddenException("Forbidden"));

        await _sut.InvokeAsync(_httpContext, next);

        Assert.That(_httpContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
    }

    [Test]
    public async Task InvokeAsync_WithGenericException_ReturnsInternalServerErrorStatusCode()
    {
        var next = new RequestDelegate(_ => throw new Exception("Generic error"));

        await _sut.InvokeAsync(_httpContext, next);

        _httpContext.Response.Body.Position = 0;
        using var result = await JsonDocument.ParseAsync(_httpContext.Response.Body);
        var root = result.RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(_httpContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
            Assert.That(root.GetProperty("message").GetString(), Is.EqualTo("INTERNAL_SERVER_ERROR"));
            Assert.That(root.TryGetProperty("source", out _), Is.False);
            Assert.That(root.GetProperty("messages").GetArrayLength(), Is.EqualTo(0));
            Assert.That(root.TryGetProperty("data", out _), Is.False);
        });
    }

    [Test]
    public async Task InvokeAsync_With_BadRequestException_ReturnsBadRequestStatusCode()
    {
        var next = new RequestDelegate(_ => throw new BadRequestException("Bad request"));
        await _sut.InvokeAsync(_httpContext, next);
        Assert.That(_httpContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
    }
}
