namespace ECS.Domain.Enums;

/// <summary>Categories of system-generated alerts.</summary>
public enum AlertType
{
    CriticalStock = 1,    // a part dropped to/below its minimum stock level
    MaintenanceDue = 2,   // a vehicle's scheduled maintenance is approaching
    DocumentExpiry = 3    // a document is approaching its expiry date
}
