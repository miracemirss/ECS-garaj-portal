using ECS.Application.Common.Interfaces;

namespace ECS.Application.Features.Reports.Models;

/// <summary>
/// Fully-resolved data model for the corporate maintenance/repair PDF. The
/// Application layer builds it (DB lookups + calculations); the Infrastructure
/// QuestPDF document is a pure renderer over this immutable model.
/// </summary>
public sealed class MaintenanceReportModel : IPdfDocumentModel
{
    public ReportHeaderModel Header { get; init; } = new();
    public ReportCompanyModel Company { get; init; } = new();
    public ReportVehicleModel? Vehicle { get; init; }
    public ReportTrailerModel? Trailer { get; init; }
    public ReportWorkOrderModel WorkOrder { get; init; } = new();
    public ReportOdometerModel Odometer { get; init; } = new();
    public IReadOnlyList<ReportTaskLine> Tasks { get; init; } = [];
    public IReadOnlyList<ReportPartLine> Parts { get; init; } = [];
    public ReportCostSummaryModel Cost { get; init; } = new();
    public string? NextMaintenanceRecommendation { get; init; }
    public string? Notes { get; init; }

    /// <summary>Payload encoded into the QR code (verification URL with report + work-order ids).</summary>
    public string VerificationPayload { get; init; } = string.Empty;
}

/// <summary>Report identity block (title + numbers + dates) shown top-right.</summary>
public sealed class ReportHeaderModel
{
    public string Title { get; init; } = "Araç Bakım / Tamir Raporu";
    public string ReportNo { get; init; } = string.Empty;
    public DateTime ReportDateUtc { get; init; }
    public string WorkOrderNo { get; init; } = string.Empty;
}

public sealed class ReportCompanyModel
{
    public string Name { get; init; } = string.Empty;
    public string? LegalName { get; init; }
    public string? Address { get; init; }
    public string? TaxOffice { get; init; }
    public string? TaxNo { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Website { get; init; }
}

public sealed class ReportVehicleModel
{
    public string Plate { get; init; } = string.Empty;
    public string Brand { get; init; } = string.Empty;
    public string? Model { get; init; }
    public string? Vin { get; init; }
    public int CurrentOdometerKm { get; init; }
}

public sealed class ReportTrailerModel
{
    public string Plate { get; init; } = string.Empty;
    public string? Type { get; init; }
    public string? Vin { get; init; }
}

public sealed class ReportWorkOrderModel
{
    public string MaintenanceType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime? ServiceInAtUtc { get; init; }
    public DateTime? CompletedAtUtc { get; init; }
    public string DurationText { get; init; } = "-";
    public string? Technician { get; init; }
    public string? ServiceProvider { get; init; }
    public string? Description { get; init; }
}

public sealed class ReportOdometerModel
{
    public int? BeforeKm { get; init; }
    public int? AfterKm { get; init; }
    public int? DifferenceKm { get; init; }
}

public sealed record ReportTaskLine(int Index, string Description, decimal? LaborHours, bool IsCompleted);

public sealed record ReportPartLine(int Index, string PartNo, string Name, decimal Quantity, string Unit, decimal UnitCost, decimal LineTotal);

/// <summary>Cost summary footer of the report (parts + labor + other, then VAT and grand total).</summary>
public sealed class ReportCostSummaryModel
{
    public decimal PartsTotal { get; init; }
    public decimal LaborCost { get; init; }
    public decimal OtherCost { get; init; }
    public decimal VatRate { get; init; }
    public decimal Subtotal => PartsTotal + LaborCost + OtherCost;
    public decimal VatAmount { get; init; }
    public decimal GrandTotal { get; init; }
    public string Currency { get; init; } = "TRY";
}
