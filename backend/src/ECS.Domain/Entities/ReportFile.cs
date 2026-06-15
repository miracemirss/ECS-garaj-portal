using ECS.Domain.Common;
using ECS.Domain.Exceptions;

namespace ECS.Domain.Entities;

/// <summary>Metadata for a generated file (maintenance PDF report, Excel export, ...).</summary>
public class ReportFile : BaseEntity
{
    private ReportFile() { }

    private ReportFile(string reportType, string filePath, string fileName, string contentType)
    {
        ReportType = reportType;
        FilePath = filePath;
        FileName = fileName;
        ContentType = contentType;
        GeneratedAt = DateTime.UtcNow;
    }

    public string ReportType { get; private set; } = null!;   // 'MaintenanceReport', 'MonthlyCost', ...
    public Guid? WorkOrderId { get; private set; }
    public string FilePath { get; private set; } = null!;
    public string FileName { get; private set; } = null!;
    public string ContentType { get; private set; } = "application/pdf";
    public long? SizeBytes { get; private set; }
    public string? Parameters { get; private set; }           // JSONB (serialized)
    public DateTime GeneratedAt { get; private set; }
    public Guid? GeneratedBy { get; private set; }

    public static ReportFile Create(
        string reportType, string filePath, string fileName,
        string contentType = "application/pdf",
        Guid? workOrderId = null, long? sizeBytes = null, Guid? generatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(reportType))
        {
            throw new DomainException("Report type is required.");
        }
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new DomainException("File path is required.");
        }
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new DomainException("File name is required.");
        }
        if (sizeBytes is < 0)
        {
            throw new DomainException("Size cannot be negative.");
        }

        return new ReportFile(reportType.Trim(), filePath, fileName, contentType)
        {
            WorkOrderId = workOrderId,
            SizeBytes = sizeBytes,
            GeneratedBy = generatedBy
        };
    }
}
