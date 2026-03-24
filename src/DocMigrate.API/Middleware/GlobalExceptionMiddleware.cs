using System.Net;
using System.Text.Json;
using FluentValidation;

namespace DocMigrate.API.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment environment)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            KeyNotFoundException ex => (HttpStatusCode.NotFound, ex.Message),
            InvalidOperationException ex => (HttpStatusCode.BadRequest, ex.Message),
            UnauthorizedAccessException ex => (HttpStatusCode.Unauthorized, ex.Message),
            ValidationException => (HttpStatusCode.BadRequest, "Erro de validacao."),
            _ => (HttpStatusCode.InternalServerError, "Ocorreu um erro interno. Tente novamente mais tarde."),
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            logger.LogError(exception, "Erro nao tratado: {Message}", exception.Message);
        }
        else
        {
            logger.LogWarning(exception, "Excecao tratada ({StatusCode}): {Message}", (int)statusCode, exception.Message);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        object response;

        if (exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            response = environment.IsDevelopment()
                ? new { message, errors, detail = exception.ToString() }
                : (object)new { message, errors };
        }
        else
        {
            response = environment.IsDevelopment()
                ? new { message, errors = (object?)null, detail = exception.ToString() }
                : (object)new { message, errors = (object?)null };
        }

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
