using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.WebUtilities;

namespace Js.LedgerEs.ErrorHandling;

/// <summary>
/// Contains the methods used to register the custom application error handling logic.
/// </summary>
public static class ErrorHandlingDependencyInjection
{
    /// <summary>
    /// Register the custom application error handling logic.
    /// </summary>
    /// <param name="app">
    /// The application builder.
    /// </param>
    /// <returns>
    /// The application builder for chaining.
    /// </returns>
    public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder app)
        => app
            .UseExceptionHandler(new ExceptionHandlerOptions
            {
                AllowStatusCode404Response = true,
                ExceptionHandler = ExceptionHandler,
            })
            .UseMiddleware<ApplicationExceptionHandlingMiddleware>();

    /// <summary>
    /// Custom exception handler used when <see cref="ApplicationExceptionHandlingMiddleware" /> isn't invoked.
    /// This will be for system exceptions not caught and wrapped in application-specific exceptions.
    /// </summary>
    /// <param name="context">
    /// The http context.
    /// </param>
    private static async Task ExceptionHandler(HttpContext context)
    {
        // Pass-through status codes from BadHttpRequestException
        var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
        var error = exceptionHandlerFeature?.Error;

        if (error is BadHttpRequestException badRequestEx)
        {
            context.Response.StatusCode = badRequestEx.StatusCode;
        }

        if (context.RequestServices.GetRequiredService<IProblemDetailsService>() is { } problemDetailsService)
        {
            await problemDetailsService.WriteAsync(new()
            {
                HttpContext = context,
                AdditionalMetadata = exceptionHandlerFeature?.Endpoint?.Metadata,
                ProblemDetails = { Status = context.Response.StatusCode }
            });
        }
        else if (ReasonPhrases.GetReasonPhrase(context.Response.StatusCode) is { } reasonPhrase)
        {
            await context.Response.WriteAsync(reasonPhrase);
        }
    }
}
