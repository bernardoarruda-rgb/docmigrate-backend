using System.Text.Json;
using DocMigrate.API.Middleware;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DocMigrate.Tests.Middleware;

public class GlobalExceptionMiddlewareTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static (GlobalExceptionMiddleware middleware, DefaultHttpContext httpContext) CreateMiddleware(
        RequestDelegate next,
        bool isDevelopment = false)
    {
        var logger = NullLogger<GlobalExceptionMiddleware>.Instance;
        var environment = new Mock<IHostEnvironment>();
        environment.Setup(e => e.EnvironmentName)
            .Returns(isDevelopment ? Environments.Development : Environments.Production);

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var middleware = new GlobalExceptionMiddleware(next, logger, environment.Object);
        return (middleware, httpContext);
    }

    private static async Task<JsonDocument> ReadResponseBodyAsync(DefaultHttpContext httpContext)
    {
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();
        return JsonDocument.Parse(body);
    }

    #region Exception type mapping

    [Fact]
    public async Task InvokeAsync_KeyNotFoundException_Returns404()
    {
        // Arrange
        RequestDelegate next = _ => throw new KeyNotFoundException("Recurso nao encontrado");
        var (middleware, httpContext) = CreateMiddleware(next);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);

        var body = await ReadResponseBodyAsync(httpContext);
        body.RootElement.GetProperty("message").GetString().Should().Be("Recurso nao encontrado");
    }

    [Fact]
    public async Task InvokeAsync_InvalidOperationException_Returns400()
    {
        // Arrange
        RequestDelegate next = _ => throw new InvalidOperationException("Operacao invalida");
        var (middleware, httpContext) = CreateMiddleware(next);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var body = await ReadResponseBodyAsync(httpContext);
        body.RootElement.GetProperty("message").GetString().Should().Be("Operacao invalida");
    }

    [Fact]
    public async Task InvokeAsync_UnauthorizedAccessException_Returns401()
    {
        // Arrange
        RequestDelegate next = _ => throw new UnauthorizedAccessException("Acesso negado");
        var (middleware, httpContext) = CreateMiddleware(next);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

        var body = await ReadResponseBodyAsync(httpContext);
        body.RootElement.GetProperty("message").GetString().Should().Be("Acesso negado");
    }

    [Fact]
    public async Task InvokeAsync_ValidationException_Returns400WithFieldErrors()
    {
        // Arrange
        var validationFailures = new List<ValidationFailure>
        {
            new("Title", "O titulo e obrigatorio"),
            new("Title", "O titulo deve ter no maximo 255 caracteres"),
            new("Description", "A descricao e muito longa"),
        };
        RequestDelegate next = _ => throw new ValidationException(validationFailures);
        var (middleware, httpContext) = CreateMiddleware(next);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var body = await ReadResponseBodyAsync(httpContext);
        body.RootElement.GetProperty("message").GetString().Should().Be("Erro de validacao.");

        var errors = body.RootElement.GetProperty("errors");
        errors.TryGetProperty("Title", out var titleErrors).Should().BeTrue();
        titleErrors.GetArrayLength().Should().Be(2);

        errors.TryGetProperty("Description", out var descErrors).Should().BeTrue();
        descErrors.GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task InvokeAsync_UnhandledException_Returns500()
    {
        // Arrange
        RequestDelegate next = _ => throw new Exception("Erro inesperado");
        var (middleware, httpContext) = CreateMiddleware(next);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

        var body = await ReadResponseBodyAsync(httpContext);
        body.RootElement.GetProperty("message").GetString()
            .Should().Be("Ocorreu um erro interno. Tente novamente mais tarde.");
    }

    #endregion

    #region Stack trace exposure

    [Fact]
    public async Task InvokeAsync_UnhandledException_InProduction_DoesNotIncludeDetail()
    {
        // Arrange
        RequestDelegate next = _ => throw new Exception("Erro inesperado");
        var (middleware, httpContext) = CreateMiddleware(next, isDevelopment: false);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        var body = await ReadResponseBodyAsync(httpContext);
        body.RootElement.TryGetProperty("detail", out _).Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_UnhandledException_InDevelopment_IncludesDetail()
    {
        // Arrange
        RequestDelegate next = _ => throw new Exception("Erro inesperado");
        var (middleware, httpContext) = CreateMiddleware(next, isDevelopment: true);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        var body = await ReadResponseBodyAsync(httpContext);
        body.RootElement.TryGetProperty("detail", out var detail).Should().BeTrue();
        detail.GetString().Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Successful requests

    [Fact]
    public async Task InvokeAsync_SuccessfulRequest_PassesThroughUnchanged()
    {
        // Arrange
        var wasCalled = false;
        RequestDelegate next = ctx =>
        {
            wasCalled = true;
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        };
        var (middleware, httpContext) = CreateMiddleware(next);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        wasCalled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_SuccessfulRequest_DoesNotModifyResponse()
    {
        // Arrange
        RequestDelegate next = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status201Created;
            return Task.CompletedTask;
        };
        var (middleware, httpContext) = CreateMiddleware(next);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status201Created);
        httpContext.Response.Body.Length.Should().Be(0);
    }

    #endregion

    #region Content-Type

    [Fact]
    public async Task InvokeAsync_AnyException_SetsContentTypeToJson()
    {
        // Arrange
        RequestDelegate next = _ => throw new KeyNotFoundException("not found");
        var (middleware, httpContext) = CreateMiddleware(next);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        httpContext.Response.ContentType.Should().Be("application/json");
    }

    #endregion
}
