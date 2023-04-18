using System.Text.Json;

using FluentValidation;

namespace Js.LedgerEs.Validation;

public class ValidationMiddleware
{
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly RequestDelegate _next;

    public ValidationMiddleware(JsonSerializerOptions jsonOptions, RequestDelegate next)
    {
        _jsonOptions = jsonOptions;
        _next = next;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (ValidationException ex)
        {
            var response = httpContext.Response;
            var problem = new HttpValidationProblemDetails(
                ex.Errors
                    .Select(e => (e.PropertyName, e.ErrorMessage))
                    .GroupBy(t => t.PropertyName)
                    .ToDictionary(
                        k => k.Key,
                        v => v.Select(t => t.ErrorMessage).ToArray()
                    )
            );

            response.ContentType = "application/problem+json";
            response.StatusCode = 400;
            problem.Status = response.StatusCode;

            await JsonSerializer.SerializeAsync(response.Body, problem, _jsonOptions, httpContext.RequestAborted);
        }
    }
}
