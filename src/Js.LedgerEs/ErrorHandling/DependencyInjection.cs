using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.WebUtilities;

namespace Js.LedgerEs.ErrorHandling;

public static class ErrorHandlingDependencyInjection
{
    public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder app)
        => app
            .UseExceptionHandler(new ExceptionHandlerOptions
            {
                AllowStatusCode404Response = true,
                ExceptionHandler = async (HttpContext context) =>
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
            })
            .UseMiddleware<ApplicationExceptionHandlingMiddleware>();
}
