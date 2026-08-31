using System.Diagnostics;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MSTailors.Application.Common.Exceptions;

namespace MSTailors.Api.Middleware;

/// <summary>
/// Converts any unhandled exception into an RFC 7807 ProblemDetails response.
/// No stack trace, driver message, or connection string ever reaches the client.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        var problem = exception switch
        {
            ValidationException validation => BuildValidationProblem(validation),

            NotFoundException notFound => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not found",
                Detail = notFound.Message
            },

            ConflictException conflict => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = conflict.Message
            },

            AuthenticationFailedException auth => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Authentication failed",
                Detail = auth.Message
            },

            ForbiddenException forbidden => new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = forbidden.Message
            },

            // 422: the request was well-formed but cannot be satisfied by
            // current data — out of stock, expired coupon, invalid transition.
            BusinessRuleException rule => BuildBusinessRuleProblem(rule),

            OperationCanceledException => new ProblemDetails
            {
                Status = StatusCodes.Status499ClientClosedRequest,
                Title = "Request cancelled",
                Detail = "The request was cancelled by the client."
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                // In Development the message helps; in Production it may leak internals.
                Detail = _environment.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred. Please try again later."
            }
        };

        problem.Instance = context.Request.Path;
        problem.Type = $"https://httpstatuses.io/{problem.Status}";
        problem.Extensions["traceId"] = traceId;

        // Expected, client-caused conditions are noise at Error level. Only a
        // genuine 5xx indicates something wrong with the server.
        if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "Unhandled exception on {Method} {Path} (traceId {TraceId})",
                context.Request.Method, context.Request.Path, traceId);
        }
        else
        {
            _logger.LogInformation(
                "{Status} on {Method} {Path}: {Detail} (traceId {TraceId})",
                problem.Status, context.Request.Method, context.Request.Path, problem.Detail, traceId);
        }

        if (context.Response.HasStarted)
        {
            // Too late to rewrite the response; the log above is the record.
            _logger.LogWarning("Response already started, cannot write ProblemDetails for {TraceId}", traceId);
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }

    private static ProblemDetails BuildBusinessRuleProblem(BusinessRuleException exception)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "The request could not be completed",
            Detail = exception.Message
        };

        if (exception.Details.Count > 0)
        {
            problem.Extensions["details"] = exception.Details;
        }

        return problem;
    }

    private static ValidationProblemDetails BuildValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred."
        };
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionHandlingMiddleware>();
}
