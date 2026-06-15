using ECS.Application.Common.Interfaces;
using ECS.Application.Features.Reports.Dtos;
using ECS.Application.Features.Reports.Models;
using ECS.Domain.Entities;
using ECS.Domain.Enums;
using ECS.Shared.Results;

namespace ECS.Application.Features.Reports;

public sealed class ReportService : IReportService
{
    private readonly IRepository<MaintenanceWorkOrder> _workOrders;
    private readonly IRepository<WorkOrderPart> _workOrderParts;
    private readonly IRepository<Part> _parts;
    private readonly IRepository<Vehicle> _vehicles;
    private readonly IRepository<Trailer> _trailers;
    private readonly IRepository<ReportFile> _reports;
    private readonly IPdfService _pdf;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogService _audit;

    public ReportService(
        IRepository<MaintenanceWorkOrder> workOrders,
        IRepository<WorkOrderPart> workOrderParts,
        IRepository<Part> parts,
        IRepository<Vehicle> vehicles,
        IRepository<Trailer> trailers,
        IRepository<ReportFile> reports,
        IPdfService pdf,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IAuditLogService audit)
    {
        _workOrders = workOrders;
        _workOrderParts = workOrderParts;
        _parts = parts;
        _vehicles = vehicles;
        _trailers = trailers;
        _reports = reports;
        _pdf = pdf;
        _uow = uow;
        _currentUser = currentUser;
        _audit = audit;
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

        var lines = await _workOrderParts.ListAsync(p => p.WorkOrderId == workOrderId, ct);
        var partIds = lines.Select(l => l.PartId).Distinct().ToList();
        var partEntities = await _parts.ListAsync(p => partIds.Contains(p.Id), ct);
        var nameById = partEntities.ToDictionary(p => p.Id, p => p.Name);

        var plate = await ResolveTargetPlateAsync(wo, ct);

        var model = new MaintenanceReportModel
        {
            WorkOrderNo = wo.WorkOrderNo ?? wo.Id.ToString(),
            TargetPlate = plate,
            Title = wo.Title,
            Status = wo.Status.ToString(),
            MaintenanceType = wo.MaintenanceType.ToString(),
            CompletedAtUtc = wo.CompletedAt,
            OdometerBeforeKm = wo.OdometerBeforeKm,
            OdometerAfterKm = wo.OdometerAfterKm,
            LaborCost = wo.LaborCost,
            PartsCost = wo.PartsCost,
            TotalCost = wo.TotalCost,
            Parts = lines
                .Select(l => new MaintenanceReportLine(nameById.GetValueOrDefault(l.PartId, "?"), l.Quantity, l.UnitCost, l.LineTotal))
                .ToList()
        };

        var bytes = _pdf.Render(model);

        var fileName = $"maintenance-{model.WorkOrderNo}.pdf";
        var report = ReportFile.Create(
            "MaintenanceReport", $"reports/{fileName}", fileName, "application/pdf",
            wo.Id, bytes.LongLength, _currentUser.UserId);

        await _reports.AddAsync(report, ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("MaintenanceReportGenerated", nameof(ReportFile), report.Id.ToString(), new { wo.WorkOrderNo }, ct);

        return Result.Success(MapReport(report));
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

    private async Task<string> ResolveTargetPlateAsync(MaintenanceWorkOrder wo, CancellationToken ct)
    {
        if (wo.TargetType == TargetType.Vehicle && wo.VehicleId is { } vid)
        {
            return (await _vehicles.GetByIdAsync(vid, ct))?.PlateNo ?? string.Empty;
        }
        if (wo.TargetType == TargetType.Trailer && wo.TrailerId is { } tid)
        {
            return (await _trailers.GetByIdAsync(tid, ct))?.PlateNo ?? string.Empty;
        }
        return string.Empty;
    }

    private static ReportFileDto MapReport(ReportFile r) => new(
        r.Id, r.ReportType, r.FileName, r.ContentType, r.SizeBytes, r.GeneratedAt);
}
