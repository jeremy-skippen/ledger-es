using System.Text.Json;

using Microsoft.AspNetCore.Mvc;

namespace Js.LedgerEs.ErrorHandling;

/// <summary>
/// Custom middleware that handles any application exception (<see cref="LedgerEsException"/>) and serializes an
/// equivalent <see cref="ProblemDetails"/> object to the HTTP response.
/// The middleware will also log the exception as an error.
/// </summary>
public class ApplicationExceptionHandlingMiddleware
{
    private readonly ILogger<LedgerEsException> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly RequestDelegate _next;

    public ApplicationExceptionHandlingMiddleware(
        ILogger<LedgerEsException> logger,
        JsonSerializerOptions jsonOptions,
        RequestDelegate next)

    {
        _logger = logger;
        _jsonOptions = jsonOptions;
        _next = next;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (LedgerEsException ex)
        {
            _logger.LogError(ex, "{Message}", ex.Message);

            var response = httpContext.Response;
            var problem = new ProblemDetails()
            {
                Detail = ex.Message,
                Status = ex.HttpStatusCode,
            };

            response.ContentType = "application/problem+json";
            response.StatusCode = problem.Status ?? 400;

            await JsonSerializer.SerializeAsync(response.Body, problem, _jsonOptions, httpContext.RequestAborted);
        }
    }
}
