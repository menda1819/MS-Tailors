using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MSTailors.Application.Health;

namespace MSTailors.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers Application services and every FluentValidation validator in
    /// this assembly. New validators are picked up by assembly scanning — there
    /// is no registration list to keep in sync.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IHealthService, HealthService>();
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly(), includeInternalTypes: true);
        return services;
    }
}
