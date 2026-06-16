using ECS.Application.Common.Interfaces;
using ECS.Application.Common.Options;
using ECS.Application.Features.Reports.Dtos;
using ECS.Application.Features.Reports.Models;
using ECS.Domain.Entities;
using ECS.Domain.Enums;
using ECS.Shared.Results;
using Microsoft.Extensions.Options;

namespace ECS.Application.Features.Reports;

public sealed class ReportService : IReportService
{
    private readonly IRepository<MaintenanceWorkOrder> _workOrders;
    private readonly IRepository<WorkOrderPart> _workOrderParts;
    private readonly IRepository<MaintenanceTask> _tasks;
    private readonly IRepository<Part> _parts;
    private readonly IRepository<Vehicle> _vehicles;
    private readonly IRepository<Trailer> _trailers;
    private readonly IRepository<Supplier> _suppliers;
    private readonly IRepository<User> _users;
    private readonly IRepository<CompanySetting> _companySettings;
    private readonly IRepository<ReportFile> _reports;
    private readonly IPdfService _pdf;
    private readonly IFileStorage _storage;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogService _audit;
    private readonly ReportingOptions _options;

    public ReportService(
        IRepository<MaintenanceWorkOrder> workOrders,
        IRepository<WorkOrderPart> workOrderParts,
        IRepository<MaintenanceTask> tasks,
        IRepository<Part> parts,
        IRepository<Vehicle> vehicles,
        IRepository<Trailer> trailers,
        IRepository<Supplier> suppliers,
        IRepository<User> users,
        IRepository<CompanySetting> companySettings,
        IRepository<ReportFile> reports,
        IPdfService pdf,
        IFileStorage storage,
        IUnitOfWork uow,
        IDateTimeProvider clock,
        ICurrentUserService currentUser,
        IAuditLogService audit,
        IOptions<ReportingOptions> options)
    {
        _workOrders = workOrders;
        _workOrderParts = workOrderParts;
        _tasks = tasks;
        _parts = parts;
        _vehicles = vehicles;
        _trailers = trailers;
        _suppliers = suppliers;
        _users = users;
        _companySettings = companySettings;
        _reports = reports;
        _pdf = pdf;
        _storage = storage;
        _uow = uow;
        _clock = clock;
        _currentUser = currentUser;
        _audit = audit;
        _options = options.Value;
    }

    public async Task<Result<ReportFileDto>> GenerateMaintenanceReportAsync(Guid workOrderId, CancellationToken ct = default)
    {
        var wo = await _workOrders.GetByIdAsync(workOrderId, ct);
        if (wo is null || wo.IsDeleted)
        {
            return Result.Failure<ReportFileDto>(Error.NotFound($"Work order {workOrderId} was not found."));
        }
        if (wo.Status != WorkOrderStatus.Completed)
        {
            return Result.Failure<ReportFileDto>(Error.Conflict("Maintenance report is only final for a completed work order."));
        }

        var model = await BuildModelAsync(wo, ct);
        var bytes = _pdf.Render(model);

        var now = _clock.UtcNow;
        var safeNo = (wo.WorkOrderNo ?? wo.Id.ToString()).Replace('/', '-');
        var relativePath = $"{_options.StorageFolder}/{now:yyyy}/{now:MM}/{safeNo}-{now:yyyyMMddHHmmss}.pdf";
        var downloadName = $"bakim-raporu-{safeNo}.pdf";

        var storedPath = await _storage.SaveAsync(relativePath, bytes, ct);

        var report = ReportFile.Create(
            "MaintenanceReport", storedPath, downloadName, "application/pdf",
            wo.Id, bytes.LongLength, _currentUser.UserId);

        await _reports.AddAsync(report, ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("MaintenanceReportGenerated", nameof(ReportFile), report.Id.ToString(),
            new { wo.WorkOrderNo, report.FileName, report.SizeBytes }, ct);

        return Result.Success(MapReport(report));
    }

    public async Task<Result<IReadOnlyList<ReportFileDto>>> GetReportsForWorkOrderAsync(Guid workOrderId, CancellationToken ct = default)
    {
        var files = await _reports.ListAsync(r => r.WorkOrderId == workOrderId, ct);
        IReadOnlyList<ReportFileDto> dtos = files
            .OrderByDescending(r => r.GeneratedAt)
            .Select(MapReport)
            .ToList();
        return Result.Success(dtos);
    }

    public async Task<Result<ReportFileContentDto>> GetReportFileAsync(Guid reportFileId, CancellationToken ct = default)
    {
        var report = await _reports.GetByIdAsync(reportFileId, ct);
        if (report is null)
        {
            return Result.Failure<ReportFileContentDto>(Error.NotFound($"Report file {reportFileId} was not found."));
        }

        var bytes = await _storage.ReadAsync(report.FilePath, ct);

        // Resilience: if the stored file was pruned, re-render it from the (still completed)
        // work order and write it back to the same path so future downloads hit storage.
        if (bytes is null && report.WorkOrderId is { } woId)
        {
            var wo = await _workOrders.GetByIdAsync(woId, ct);
            if (wo is { IsDeleted: false, Status: WorkOrderStatus.Completed })
            {
                bytes = _pdf.Render(await BuildModelAsync(wo, ct));
                await _storage.SaveAsync(report.FilePath, bytes, ct);
            }
        }

        if (bytes is null)
        {
            return Result.Failure<ReportFileContentDto>(Error.NotFound("The report file content is no longer available."));
        }

        return Result.Success(new ReportFileContentDto(report.Id, report.FileName, report.ContentType, bytes));
    }

    public async Task<Result<IReadOnlyList<VehicleCostSummaryDto>>> GetVehicleCostSummariesAsync(CancellationToken ct = default)
    {
        var completed = await _workOrders.ListAsync(
            w => !w.IsDeleted && w.Status == WorkOrderStatus.Completed && w.TargetType == TargetType.Vehicle, ct);

        var groups = completed.Where(w => w.VehicleId != null).GroupBy(w => w.VehicleId!.Value).ToList();
        var vehicleIds = groups.Select(g => g.Key).ToList();
        var vehicles = await _vehicles.ListAsync(v => vehicleIds.Contains(v.Id), ct);
        var plateById = vehicles.ToDictionary(v => v.Id, v => v.PlateNo);

        IReadOnlyList<VehicleCostSummaryDto> dtos = groups
            .Select(g => new VehicleCostSummaryDto(g.Key, plateById.GetValueOrDefault(g.Key, string.Empty), g.Count(), g.Sum(w => w.TotalCost)))
            .ToList();

        return Result.Success(dtos);
    }

    public async Task<Result<IReadOnlyList<MonthlyCostDto>>> GetMonthlyCostsAsync(CancellationToken ct = default)
    {
        var completed = await _workOrders.ListAsync(
            w => !w.IsDeleted && w.Status == WorkOrderStatus.Completed && w.CompletedAt != null, ct);

        IReadOnlyList<MonthlyCostDto> dtos = completed
            .GroupBy(w => new { w.CompletedAt!.Value.Year, w.CompletedAt!.Value.Month })
            .Select(g => new MonthlyCostDto(g.Key.Year, g.Key.Month, g.Count(), g.Sum(w => w.TotalCost)))
            .OrderBy(d => d.Year).ThenBy(d => d.Month)
            .ToList();

        return Result.Success(dtos);
    }

    private async Task<MaintenanceReportModel> BuildModelAsync(MaintenanceWorkOrder wo, CancellationToken ct)
    {
        var lines = await _workOrderParts.ListAsync(p => p.WorkOrderId == wo.Id, ct);
        var partIds = lines.Select(l => l.PartId).Distinct().ToList();
        var partEntities = await _parts.ListAsync(p => partIds.Contains(p.Id), ct);
        var partById = partEntities.ToDictionary(p => p.Id);

        var taskEntities = await _tasks.ListAsync(t => t.WorkOrderId == wo.Id, ct);

        ReportVehicleModel? vehicleModel = null;
        ReportTrailerModel? trailerModel = null;
        Vehicle? vehicle = null;
        if (wo.TargetType == TargetType.Vehicle && wo.VehicleId is { } vid)
        {
            vehicle = await _vehicles.GetByIdAsync(vid, ct);
            if (vehicle is not null)
            {
                vehicleModel = new ReportVehicleModel
                {
                    Plate = vehicle.PlateNo,
                    Brand = vehicle.Brand,
                    Model = vehicle.Model,
                    Vin = vehicle.Vin,
                    CurrentOdometerKm = vehicle.CurrentOdometerKm
                };
            }
        }
        else if (wo.TargetType == TargetType.Trailer && wo.TrailerId is { } tid)
        {
            var trailer = await _trailers.GetByIdAsync(tid, ct);
            if (trailer is not null)
            {
                trailerModel = new ReportTrailerModel { Plate = trailer.PlateNo, Type = trailer.TrailerType, Vin = trailer.Vin };
            }
        }

        string? technician = wo.AssignedTo is { } techId ? (await _users.GetByIdAsync(techId, ct))?.FullName : null;
        string? serviceProvider = wo.SupplierId is { } supId ? (await _suppliers.GetByIdAsync(supId, ct))?.Name : null;

        var companyName = (await _companySettings.FirstOrDefaultAsync(_ => true, ct))?.CompanyName ?? _options.Company.Name;

        var serviceInAt = wo.StartedAt ?? wo.CreatedAt;
        var odoDiff = wo.OdometerBeforeKm is { } b && wo.OdometerAfterKm is { } a ? a - b : (int?)null;

        var vatRate = _options.VatRate;
        var subtotal = wo.TotalCost;                          // labor + parts (matches the WO total)
        var vatAmount = decimal.Round(subtotal * vatRate, 2);
        var grandTotal = subtotal + vatAmount;
        var currency = string.IsNullOrWhiteSpace(_options.DefaultCurrency) ? "TRY" : _options.DefaultCurrency;

        var now = _clock.UtcNow;

        return new MaintenanceReportModel
        {
            Header = new ReportHeaderModel
            {
                Title = "Araç Bakım / Tamir Raporu",
                ReportNo = $"RPR-{wo.WorkOrderNo ?? wo.Id.ToString()[..8]}",
                ReportDateUtc = now,
                WorkOrderNo = wo.WorkOrderNo ?? wo.Id.ToString()
            },
            Company = new ReportCompanyModel
            {
                Name = companyName,
                LegalName = _options.Company.LegalName,
                Address = _options.Company.Address,
                TaxOffice = _options.Company.TaxOffice,
                TaxNo = _options.Company.TaxNo,
                Phone = _options.Company.Phone,
                Email = _options.Company.Email,
                Website = _options.Company.Website
            },
            Vehicle = vehicleModel,
            Trailer = trailerModel,
            WorkOrder = new ReportWorkOrderModel
            {
                MaintenanceType = TranslateMaintenanceType(wo.MaintenanceType),
                Status = TranslateStatus(wo.Status),
                ServiceInAtUtc = serviceInAt,
                CompletedAtUtc = wo.CompletedAt,
                DurationText = FormatDuration(serviceInAt, wo.CompletedAt),
                Technician = technician,
                ServiceProvider = serviceProvider,
                Description = wo.Description
            },
            Odometer = new ReportOdometerModel
            {
                BeforeKm = wo.OdometerBeforeKm,
                AfterKm = wo.OdometerAfterKm,
                DifferenceKm = odoDiff
            },
            Tasks = taskEntities
                .OrderBy(t => t.SortOrder)
                .Select((t, i) => new ReportTaskLine(i + 1, t.Description, t.LaborHours, t.IsCompleted))
                .ToList(),
            Parts = lines
                .Select((l, i) =>
                {
                    partById.TryGetValue(l.PartId, out var p);
                    return new ReportPartLine(i + 1, p?.PartNo ?? "-", p?.Name ?? "?", l.Quantity, p?.Unit ?? "adet", l.UnitCost, l.LineTotal);
                })
                .ToList(),
            Cost = new ReportCostSummaryModel
            {
                PartsTotal = wo.PartsCost,
                LaborCost = wo.LaborCost,
                OtherCost = 0m,
                VatRate = vatRate,
                VatAmount = vatAmount,
                GrandTotal = grandTotal,
                Currency = currency
            },
            NextMaintenanceRecommendation = BuildNextMaintenance(vehicle),
            Notes = wo.Description,
            VerificationPayload = $"{_options.VerificationBaseUrl}?wo={wo.WorkOrderNo ?? wo.Id.ToString()}"
        };
    }

    private static string? BuildNextMaintenance(Vehicle? vehicle)
    {
        if (vehicle is null)
        {
            return null;
        }
        var parts = new List<string>();
        if (vehicle.NextMaintenanceKm is { } km)
        {
            parts.Add($"{km:N0} km");
        }
        if (vehicle.NextMaintenanceDate is { } date)
        {
            parts.Add(date.ToString("dd.MM.yyyy"));
        }
        return parts.Count == 0 ? null : "Sonraki planlı bakım: " + string.Join(" veya ", parts);
    }

    private static string FormatDuration(DateTime? from, DateTime? to)
    {
        if (from is null || to is null || to < from)
        {
            return "-";
        }
        var span = to.Value - from.Value;
        if (span.TotalDays >= 1)
        {
            return $"{(int)span.TotalDays} gün {span.Hours} saat";
        }
        return span.TotalHours >= 1 ? $"{(int)span.TotalHours} saat {span.Minutes} dk" : $"{span.Minutes} dk";
    }

    private static string TranslateMaintenanceType(WorkOrderType type) => type switch
    {
        WorkOrderType.Preventive => "Periyodik Bakım",
        WorkOrderType.Corrective => "Arıza / Tamir",
        WorkOrderType.Inspection => "Muayene / Kontrol",
        WorkOrderType.Tire => "Lastik",
        _ => "Diğer"
    };

    private static string TranslateStatus(WorkOrderStatus status) => status switch
    {
        WorkOrderStatus.Draft => "Taslak",
        WorkOrderStatus.Open => "Açık",
        WorkOrderStatus.InProgress => "İşlemde",
        WorkOrderStatus.Completed => "Tamamlandı",
        WorkOrderStatus.Cancelled => "İptal",
        _ => status.ToString()
    };

    private static ReportFileDto MapReport(ReportFile r) => new(
        r.Id, r.ReportType, r.WorkOrderId, r.FileName, r.ContentType, r.SizeBytes, r.GeneratedAt);
}
