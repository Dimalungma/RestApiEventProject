using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace RestApiEventProject.Presentation.Middleware;
/// <summary>
/// Middleware для обработки исключений, использует ProblemDetails
/// </summary>
public class CustomExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CustomExceptionHandler> _logger;

    public CustomExceptionHandler(
        RequestDelegate next,
        ILogger<CustomExceptionHandler> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, $"Middleware перехватил необработанное исключение. Path: {context.Request.Path}");

            await HandleExceptionAsync(context, exception);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = GetStatusCodeAndTitle(exception);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private static (int statusCode, string title) GetStatusCodeAndTitle(Exception exception)
    {
        return exception switch
        {
            ArgumentException => ((int)HttpStatusCode.BadRequest, "Ошибка валидации, неправильные аргументы"),
            InvalidOperationException => ((int)HttpStatusCode.BadRequest, "Ошибка валидации, недопустимая операция"),
            KeyNotFoundException => ((int)HttpStatusCode.NotFound, "Не найдено ресурсов с таким ключом"),
            _ => ((int)HttpStatusCode.InternalServerError, "Внутренняя ошибка сервера, обратитесь к разработчику")
        };
    }
}