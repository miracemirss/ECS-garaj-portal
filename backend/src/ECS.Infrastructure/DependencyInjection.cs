using ECS.Application.Common.Interfaces;
using ECS.Infrastructure.Excel;
using ECS.Infrastructure.Identity;
using ECS.Infrastructure.Pdf;
using ECS.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECS.Infrastructure;

/// <summary>
/// Registers technical services (PDF, Excel, auth, clock) and background jobs.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IPdfService, QuestPdfReportService>();
        services.AddScoped<IExcelService, ClosedXmlExportService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // Background jobs are registered here as they are built, e.g.:
        // services.AddHostedService<CriticalStockAlertJob>();
        // services.AddHostedService<MaintenanceDueAlertJob>();

        return services;
    }
}
