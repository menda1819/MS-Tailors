using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MSTailors.Api.Filters;

/// <summary>
/// Runs the FluentValidation validator registered for each action argument.
/// </summary>
/// <remarks>
/// Applied globally, so validation cannot be forgotten on a new endpoint. An
/// argument type with no registered validator simply passes through.
/// </remarks>
public sealed class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _services;

    public ValidationFilter(IServiceProvider services) => _services = services;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var failures = new List<FluentValidation.Results.ValidationFailure>();

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());

            if (_services.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

            if (!result.IsValid)
            {
                failures.AddRange(result.Errors);
            }
        }

        if (failures.Count > 0)
        {
            var errors = failures
                .GroupBy(failure => Camelise(failure.PropertyName))
                .ToDictionary(group => group.Key, group => group.Select(f => f.ErrorMessage).Distinct().ToArray());

            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(errors)
            {
                Title = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest,
                Instance = context.HttpContext.Request.Path
            });

            return;
        }

        await next();
    }

    /// <summary>Matches the camelCase JSON the client sent, so errors map to fields.</summary>
    private static string Camelise(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return propertyName;
        }

        var segments = propertyName.Split('.');

        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            if (segment.Length > 0 && char.IsUpper(segment[0]))
            {
                segments[i] = char.ToLowerInvariant(segment[0]) + segment[1..];
            }
        }

        return string.Join('.', segments);
    }
}
