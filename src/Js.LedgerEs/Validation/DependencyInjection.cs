using FluentValidation;

namespace Js.LedgerEs.Validation;

public static class ValidationDependencyInjection
{
    public static IServiceCollection AddValidators(this IServiceCollection services)
        => services.AddValidatorsFromAssemblies(new[] { typeof(Program).Assembly }, ServiceLifetime.Transient);

    public static MediatRServiceConfiguration AddValidationBehavior(this MediatRServiceConfiguration cfg)
        => cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));

    public static IApplicationBuilder UseValidation(this IApplicationBuilder app)
        => app.UseMiddleware<ValidationMiddleware>();
}
