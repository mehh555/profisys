using System.Net;
using Microsoft.AspNetCore.Mvc;
using ProfisysTask.Exceptions;

namespace ProfisysTask.Middleware;

public class GlobalExceptionHandler : IMiddleware
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (BadRequestException ex)
        {
            _logger.LogWarning(ex, "Bad request");
            await WriteProblemDetailsAsync(context, HttpStatusCode.BadRequest, "Bad Request", ex.Message);
        }
        catch (CsvImportException ex)
        {
            _logger.LogWarning(ex, "CSV import validation failed with {Count} error(s)", ex.Errors.Count);
            await WriteProblemDetailsAsync(context, HttpStatusCode.BadRequest, "CSV Import Error", ex.Message, ex.Errors);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation("Request was cancelled by the client");
            context.Response.StatusCode = 499;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            var detail = _environment.IsDevelopment()
                ? ex.Message
                : "An unexpected error occurred. Please try again later.";
            await WriteProblemDetailsAsync(context, HttpStatusCode.InternalServerError, "Internal Server Error", detail);
        }
    }

    private static async Task WriteProblemDetailsAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        string title,
        string detail,
        IReadOnlyList<string>? errors = null)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{(int)statusCode}",
            Title = title,
            Status = (int)statusCode,
            Detail = detail
        };

        if (errors is not null)
            problemDetails.Extensions["errors"] = errors;

        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
