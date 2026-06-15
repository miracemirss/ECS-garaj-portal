using System.Reflection;
using ECS.Application.Features.Alerts;
using ECS.Application.Features.Assignments;
using ECS.Application.Features.Drivers;
using ECS.Application.Features.Inventory;
using ECS.Application.Features.Maintenance;
using ECS.Application.Features.Reports;
using ECS.Application.Features.Trailers;
using ECS.Application.Features.Vehicles;
using FluentValidation;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace ECS.Application;

/// <summary>
/// Registers Application-layer services, validators and the mapper.
/// Called from the API composition root.
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

        // Feature application services.
        services.AddScoped<IVehicleService, VehicleService>();
        services.AddScoped<ITrailerService, TrailerService>();
        services.AddScoped<IDriverService, DriverService>();
        services.AddScoped<IAssignmentService, AssignmentService>();
        services.AddScoped<IMaintenanceService, MaintenanceService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<IReportService, ReportService>();

        return services;
    }
}
