using ECS.Application.Common.Interfaces;

namespace ECS.Application.Features.Reports.Models;

/// <summary>Data model rendered to a PDF maintenance report by IPdfService.</summary>
public sealed class MaintenanceReportModel : IPdfDocumentModel
{
    public string WorkOrderNo { get; init; } = string.Empty;
    public string TargetPlate { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string MaintenanceType { get; init; } = string.Empty;
    public DateTime? CompletedAtUtc { get; init; }
    public int? OdometerBeforeKm { get; init; }
    public int? OdometerAfterKm { get; init; }
    public decimal LaborCost { get; init; }
    public decimal PartsCost { get; init; }
    public decimal TotalCost { get; init; }
    public IReadOnlyList<MaintenanceReportLine> Parts { get; init; } = [];
}

public sealed record MaintenanceReportLine(string PartName, decimal Quantity, decimal UnitCost, decimal LineTotal);
