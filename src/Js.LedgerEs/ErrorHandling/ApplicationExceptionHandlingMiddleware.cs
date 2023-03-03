using Microsoft.AspNetCore.Mvc;

using System.Text.Json;

namespace Js.LedgerEs.ErrorHandling;

public class ApplicationExceptionHandlingMiddleware
{
    private readonly ILogger<LedgerEsException> _logger;
    private readonly RequestDelegate _next;

    public ApplicationExceptionHandlingMiddleware(ILogger<LedgerEsException> logger, RequestDelegate next)
    {
        _logger = logger;
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
                Status = ex switch
                {
                    EventStoreConcurrencyException => 409,
                    _ => 400,
                },
            };

            response.ContentType = "application/problem+json";
            response.StatusCode = problem.Status ?? 400;

            await JsonSerializer.SerializeAsync(response.Body, problem, JsonConfig.SerializerOptions, httpContext.RequestAborted);
        }
    }
}
