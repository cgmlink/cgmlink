using System.Threading.Tasks;
using CgmLink.AspNetCore.Middleware;
using CgmLink.AspNetCore.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CgmLink.AspNetCore.Tests.Security;

[TestFixture]
internal sealed class SecurityExtensionsTests
{
    [Test]
    public async Task InvokeAsync_WithSwaggerRequest_AddsConfiguredSecurityHeaders()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/swagger/index.html";
        var sut = CreateMiddleware(
            new SecurityHeaderSettings(
                EnableReferrerPolicy: true,
                ReferrerPolicy: "strict-origin",
                EnableContentSecurityPolicy: true,
                ContentSecurityPolicy: "default-src 'self'"));

        await sut.InvokeAsync(context, _ => Task.CompletedTask);

        Assert.Multiple(() =>
        {
            Assert.That(context.Response.Headers["X-Content-Type-Options"].ToString(), Is.EqualTo("nosniff"));
            Assert.That(context.Response.Headers["X-Frame-Options"].ToString(), Is.EqualTo("SAMEORIGIN"));
            Assert.That(context.Response.Headers["Referrer-Policy"].ToString(), Is.EqualTo("strict-origin"));
            Assert.That(context.Response.Headers["Content-Security-Policy"].ToString(), Is.EqualTo("default-src 'self'"));
        });
    }

    [Test]
    public async Task InvokeAsync_OutsideSwagger_DoesNotAddOptionalHeadersWhenDisabledOrNotApplicable()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/health";
        var sut = CreateMiddleware(
            new SecurityHeaderSettings(
                EnableReferrerPolicy: false,
                EnableContentSecurityPolicy: true,
                ContentSecurityPolicy: "default-src 'self'"));

        await sut.InvokeAsync(context, _ => Task.CompletedTask);

        Assert.Multiple(() =>
        {
            Assert.That(context.Response.Headers.ContainsKey("Referrer-Policy"), Is.False);
            Assert.That(context.Response.Headers.ContainsKey("Content-Security-Policy"), Is.False);
            Assert.That(context.Response.Headers["X-Content-Type-Options"].ToString(), Is.EqualTo("nosniff"));
            Assert.That(context.Response.Headers["X-Frame-Options"].ToString(), Is.EqualTo("SAMEORIGIN"));
        });
    }

    [Test]
    public async Task InvokeAsync_WithExistingHeaders_DoesNotOverwriteThem()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/swagger/index.html";
        context.Response.Headers["X-Content-Type-Options"] = "existing";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "origin";
        context.Response.Headers["Content-Security-Policy"] = "frame-ancestors 'self'";
        var sut = CreateMiddleware(
            new SecurityHeaderSettings(
                EnableReferrerPolicy: true,
                ReferrerPolicy: "strict-origin",
                EnableContentSecurityPolicy: true,
                ContentSecurityPolicy: "default-src 'self'"));

        await sut.InvokeAsync(context, _ => Task.CompletedTask);

        Assert.Multiple(() =>
        {
            Assert.That(context.Response.Headers["X-Content-Type-Options"].ToString(), Is.EqualTo("existing"));
            Assert.That(context.Response.Headers["X-Frame-Options"].ToString(), Is.EqualTo("DENY"));
            Assert.That(context.Response.Headers["Referrer-Policy"].ToString(), Is.EqualTo("origin"));
            Assert.That(context.Response.Headers["Content-Security-Policy"].ToString(), Is.EqualTo("frame-ancestors 'self'"));
        });
    }

    private static SecurityHeadersMiddleware CreateMiddleware(SecurityHeaderSettings settings)
    {
        return new SecurityHeadersMiddleware(Options.Create(settings));
    }
}
