using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Hosting;

namespace RentBridge.Api.Middleware;

/// <summary>
/// Global exception handling. FluentValidation exceptions become 400
/// Validation ProblemDetails; everything else becomes a 500 ProblemDetails
/// (with the real message exposed only in Development).
/// Convention-based middleware: activated per-request by UseMiddleware,
/// with constructor services injected from the container.
/// </summary>
public sealed class ExceptionHandlerMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlerMiddleware> logger,
    ProblemDetailsFactory problemDetailsFactory,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            logger.LogInformation("Request validation failed: {Errors}", ex.Message);

            var modelState = new ModelStateDictionary();
            foreach (var failure in ex.Errors ?? [])
            {
                modelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
            }

            var problem = problemDetailsFactory.CreateValidationProblemDetails(
                context,
                modelState,
                statusCode: StatusCodes.Status400BadRequest,
                title: "One or more validation errors occurred.");

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(problem, context.RequestAborted);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception processing {Method} {Path}",
                context.Request.Method, context.Request.Path);

            var problem = problemDetailsFactory.CreateProblemDetails(
                context,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An error occurred while processing your request.",
                detail: environment.IsDevelopment() ? ex.Message : null);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(problem, context.RequestAborted);
        }
    }
}