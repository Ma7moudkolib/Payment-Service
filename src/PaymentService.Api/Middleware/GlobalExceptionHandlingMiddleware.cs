using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.Exceptions;
using PaymentService.Domain.Exceptions;

namespace PaymentService.Api.Middleware;

public sealed class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
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
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var problemDetails = CreateProblemDetails(context, exception);

        if (problemDetails.Status >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled API exception.");
        }
        else
        {
            _logger.LogWarning(exception, "Handled API exception.");
        }

        context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private static ProblemDetails CreateProblemDetails(HttpContext context, Exception exception)
    {
        var problemDetails = exception switch
        {
            ApplicationValidationException validationException => new ValidationProblemDetails(
                validationException.Errors.ToDictionary(error => error.Key, error => error.Value))
            {
                Title = "Validation failed.",
                Status = StatusCodes.Status400BadRequest,
                Type = "https://httpstatuses.com/400"
            },
            PaymentNotFoundException => Create("Payment was not found.", StatusCodes.Status404NotFound),
            PaymentMethodNotFoundException => Create("Payment method was not found.", StatusCodes.Status404NotFound),
            PaymentProcessingException or DomainException => Create(exception.Message, StatusCodes.Status409Conflict),
            InvalidOperationException => Create(exception.Message, StatusCodes.Status400BadRequest),
            _ => Create("An unexpected error occurred.", StatusCodes.Status500InternalServerError)
        };

        problemDetails.Instance = context.Request.Path;
        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

        return problemDetails;
    }

    private static ProblemDetails Create(string title, int statusCode) =>
        new()
        {
            Title = title,
            Status = statusCode,
            Type = $"https://httpstatuses.com/{statusCode}"
        };
}
