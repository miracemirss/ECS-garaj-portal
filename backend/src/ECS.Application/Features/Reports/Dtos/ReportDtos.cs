namespace ECS.Application.Features.Reports.Dtos;

public sealed record ReportFileDto(
    Guid Id,
    string ReportType,
    string FileName,
    string ContentType,
    long? SizeBytes,
    DateTime GeneratedAt);

public sealed record VehicleCostSummaryDto(
    Guid VehicleId,
    string PlateNo,
    int WorkOrderCount,
    decimal TotalCost);

public sealed record MonthlyCostDto(
    int Year,
    int Month,
    int WorkOrderCount,
    decimal TotalCost);
