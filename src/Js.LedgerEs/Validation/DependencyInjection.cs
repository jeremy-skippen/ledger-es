using FluentValidation;

namespace Js.LedgerEs.Validation;

public static class DependencyInjection
{
    public static IServiceCollection AddValidators(this IServiceCollection services)
        => services.AddValidatorsFromAssemblies(new[] { typeof(Program).Assembly }, ServiceLifetime.Transient);

    public static IApplicationBuilder UseValidation(this IApplicationBuilder app)
        => app.UseMiddleware<ValidationMiddleware>();

    public static MediatRServiceConfiguration AddValidationBehavior(this MediatRServiceConfiguration cfg)
        => cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
}
