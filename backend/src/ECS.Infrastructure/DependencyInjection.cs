using ECS.Application.Common.Interfaces;
using ECS.Application.Common.Options;
using ECS.Infrastructure.Excel;
using ECS.Infrastructure.Identity;
using ECS.Infrastructure.Pdf;
using ECS.Infrastructure.Services;
using ECS.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;

namespace ECS.Infrastructure;

/// <summary>
/// Registers technical services (PDF, file storage, QR, Excel, auth, clock) and
/// background jobs.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // QuestPDF Community license must be set once before any document is generated.
        QuestPDF.Settings.License = LicenseType.Community;

        services.Configure<ReportingOptions>(configuration.GetSection(ReportingOptions.SectionName));
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        // Reporting: PDF document rendering + verification QR + cached logo + file storage.
        services.AddSingleton<ReportBrandAssets>();
        services.AddSingleton<IQrCodeGenerator, QrCodeGenerator>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddScoped<IPdfService, QuestPdfReportService>();
        services.AddScoped<IExcelService, ClosedXmlExportService>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        // Background jobs are registered here as they are built, e.g.:
        // services.AddHostedService<CriticalStockAlertJob>();
        // services.AddHostedService<MaintenanceDueAlertJob>();

        return services;
    }
}
