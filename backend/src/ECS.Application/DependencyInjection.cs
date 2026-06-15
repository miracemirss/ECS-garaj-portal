using System.Reflection;
using FluentValidation;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace ECS.Application;

/// <summary>
/// Registers Application-layer services. Called from the API composition root.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // FluentValidation: auto-register every validator declared in this assembly.
        services.AddValidatorsFromAssembly(assembly);

        // Mapster: scan IRegister mapping configurations and expose IMapper.
        var typeAdapterConfig = TypeAdapterConfig.GlobalSettings;
        typeAdapterConfig.Scan(assembly);
        services.AddSingleton(typeAdapterConfig);
        services.AddScoped<IMapper, ServiceMapper>();

        // Feature application services are registered here as they are built, e.g.:
        // services.AddScoped<IVehicleService, VehicleService>();
        // services.AddScoped<IMaintenanceService, MaintenanceService>();

        return services;
    }
}
