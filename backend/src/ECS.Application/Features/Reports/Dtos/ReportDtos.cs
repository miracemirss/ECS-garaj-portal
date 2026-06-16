namespace ECS.Application.Features.Reports.Dtos;

/// <summary>Metadata for a generated report file (no binary payload).</summary>
public sealed record ReportFileDto(
    Guid Id,
    string ReportType,
    Guid? WorkOrderId,
    string FileName,
    string ContentType,
    long? SizeBytes,
    DateTime GeneratedAt);

/// <summary>A report file plus its binary content, used by the download endpoint.</summary>
public sealed record ReportFileContentDto(
    Guid Id,
    string FileName,
    string ContentType,
    byte[] Content);

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
