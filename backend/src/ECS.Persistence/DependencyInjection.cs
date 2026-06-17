using ECS.Application.Common.Interfaces;
using ECS.Domain.Enums;
using ECS.Persistence.Contexts;
using ECS.Persistence.Interceptors;
using ECS.Persistence.Repositories;
using ECS.Persistence.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Npgsql.NameTranslation;

namespace ECS.Persistence;

/// <summary>
/// Registers the EF Core context (PostgreSQL), repositories and unit of work.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<AuditableEntityInterceptor>();

        // Build a shared NpgsqlDataSource that maps the CLR enums onto the native
        // PostgreSQL enum types from migration 0001. The DB labels are PascalCase
        // (e.g. 'InMaintenance'), so a null name translator is used to keep the
        // enum member names verbatim instead of the default snake_case.
        var connectionString = configuration.GetConnectionString("Default");
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        var keepNames = new NpgsqlNullNameTranslator();

        dataSourceBuilder.MapEnum<VehicleStatus>("asset_status", keepNames);
        dataSourceBuilder.MapEnum<TrailerStatus>("asset_status", keepNames);
        dataSourceBuilder.MapEnum<DriverStatus>("driver_status", keepNames);
        dataSourceBuilder.MapEnum<AssignmentStatus>("assignment_status", keepNames);
        dataSourceBuilder.MapEnum<TargetType>("work_order_target_type", keepNames);
        dataSourceBuilder.MapEnum<WorkOrderType>("maintenance_type", keepNames);
        dataSourceBuilder.MapEnum<WorkOrderStatus>("work_order_status", keepNames);
        dataSourceBuilder.MapEnum<StockMovementType>("stock_movement_type", keepNames);
        dataSourceBuilder.MapEnum<AlertType>("alert_type", keepNames);
        dataSourceBuilder.MapEnum<AlertPriority>("alert_severity", keepNames);
        dataSourceBuilder.MapEnum<AlertStatus>("alert_status", keepNames);
        dataSourceBuilder.MapEnum<DocumentOwnerType>("document_owner_type", keepNames);
        dataSourceBuilder.MapEnum<DocumentType>("document_type", keepNames);

        var dataSource = dataSourceBuilder.Build();
        services.AddSingleton(dataSource);

        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(
                serviceProvider.GetRequiredService<NpgsqlDataSource>(),
                npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));

            options.AddInterceptors(serviceProvider.GetRequiredService<AuditableEntityInterceptor>());
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<INumberGenerator, NumberGenerator>();
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
