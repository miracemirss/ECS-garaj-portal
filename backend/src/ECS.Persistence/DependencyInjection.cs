using ECS.Application.Common.Interfaces;
using ECS.Domain.Enums;
using ECS.Persistence.Configurations;
using ECS.Persistence.Contexts;
using ECS.Persistence.Interceptors;
using ECS.Persistence.Repositories;
using ECS.Persistence.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace ECS.Persistence;

/// <summary>
/// Registers the EF Core context (PostgreSQL with native enum mapping + snake_case
/// naming), repositories and unit of work.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<AuditableEntityInterceptor>();

        // A single pooled data source that knows how to read/write the PostgreSQL enum
        // types. Labels are kept PascalCase to match the schema.
        services.AddSingleton(_ =>
        {
            var builder = new NpgsqlDataSourceBuilder(configuration.GetConnectionString("Default"));
            var labels = new PascalCaseNameTranslator();
            builder.MapEnum<VehicleStatus>("asset_status", labels);
            builder.MapEnum<TrailerStatus>("asset_status", labels);
            builder.MapEnum<DriverStatus>("driver_status", labels);
            builder.MapEnum<AssignmentStatus>("assignment_status", labels);
            builder.MapEnum<WorkOrderStatus>("work_order_status", labels);
            builder.MapEnum<TargetType>("work_order_target_type", labels);
            builder.MapEnum<WorkOrderType>("maintenance_type", labels);
            builder.MapEnum<StockMovementType>("stock_movement_type", labels);
            builder.MapEnum<AlertType>("alert_type", labels);
            builder.MapEnum<AlertPriority>("alert_severity", labels);
            builder.MapEnum<AlertStatus>("alert_status", labels);
            builder.MapEnum<DocumentOwnerType>("document_owner_type", labels);
            builder.MapEnum<DocumentType>("document_type", labels);
            return builder.Build();
        });

        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(
                serviceProvider.GetRequiredService<NpgsqlDataSource>(),
                npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
            options.UseSnakeCaseNamingConvention();
            options.AddInterceptors(serviceProvider.GetRequiredService<AuditableEntityInterceptor>());
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<INumberGenerator, NumberGenerator>();
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
