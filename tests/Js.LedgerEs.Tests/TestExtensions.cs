using FluentValidation;

using Js.LedgerEs.Validation;

using MediatR.Registration;

using Microsoft.Extensions.DependencyInjection;

namespace Js.LedgerEs.Tests;

internal static class TestExtensions
{
    internal static void ShouldHaveValidationErrorFor(this ValidationException ex, string propertyName, string? withMessage = null)
    {
        Assert.True(ex.Errors.Any(e => e.PropertyName == propertyName), $"Expected validation exception to contain error for property '{propertyName}");
        if (!string.IsNullOrEmpty(withMessage))
        {
            var err = ex.Errors.Where(e => e.PropertyName == propertyName).ToList();
            Assert.True(err.Any(e => e.ErrorMessage == withMessage), $"Expected error for property '{propertyName}' with message '{withMessage}'");
        }
    }

    internal static IServiceCollection AddMediatRForUnitTests(this IServiceCollection services)
    {
        var serviceConfig = new MediatRServiceConfiguration();

        serviceConfig.AddValidationBehavior();

        ServiceRegistrar.AddMediatRClasses(services, serviceConfig);
        ServiceRegistrar.AddRequiredServices(services, serviceConfig);

        return services;
    }
}
