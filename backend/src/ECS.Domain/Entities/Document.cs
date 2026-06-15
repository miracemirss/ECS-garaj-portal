using ECS.Domain.Common;
using ECS.Domain.Enums;
using ECS.Domain.Exceptions;

namespace ECS.Domain.Entities;

/// <summary>A document belonging to a vehicle, trailer, driver or the company.</summary>
public class Document : AuditableEntity
{
    private Document() { }

    private Document(DocumentOwnerType ownerType, DocumentType type, string title)
    {
        OwnerType = ownerType;
        DocumentType = type;
        Title = title;
    }

    public DocumentOwnerType OwnerType { get; private set; }
    public Guid? VehicleId { get; private set; }
    public Guid? TrailerId { get; private set; }
    public Guid? DriverId { get; private set; }
    public DocumentType DocumentType { get; private set; }
    public string Title { get; private set; } = null!;
    public string? FilePath { get; private set; }
    public DateOnly? IssueDate { get; private set; }
    public DateOnly? ExpiryDate { get; private set; }

    public static Document ForVehicle(Guid vehicleId, DocumentType type, string title)
        => Build(DocumentOwnerType.Vehicle, type, title, vehicleId, null, null);

    public static Document ForTrailer(Guid trailerId, DocumentType type, string title)
        => Build(DocumentOwnerType.Trailer, type, title, null, trailerId, null);

    public static Document ForDriver(Guid driverId, DocumentType type, string title)
        => Build(DocumentOwnerType.Driver, type, title, null, null, driverId);

    public static Document ForCompany(DocumentType type, string title)
        => Build(DocumentOwnerType.Company, type, title, null, null, null);

    private static Document Build(DocumentOwnerType ownerType, DocumentType type, string title, Guid? vehicleId, Guid? trailerId, Guid? driverId)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Document title is required.");
        }
        return new Document(ownerType, type, title.Trim())
        {
            VehicleId = vehicleId,
            TrailerId = trailerId,
            DriverId = driverId
        };
    }

    public void SetDates(DateOnly? issueDate, DateOnly? expiryDate)
    {
        if (issueDate is not null && expiryDate is not null && expiryDate < issueDate)
        {
            throw new DomainException("Expiry date cannot precede issue date.");
        }
        IssueDate = issueDate;
        ExpiryDate = expiryDate;
    }

    public void SetFile(string? filePath) => FilePath = filePath;

    public bool IsExpiringWithin(int days, DateOnly today)
        => ExpiryDate is not null && ExpiryDate.Value <= today.AddDays(days);
}
