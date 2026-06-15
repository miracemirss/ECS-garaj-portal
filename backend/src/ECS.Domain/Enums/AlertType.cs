namespace ECS.Domain.Enums;

/// <summary>
/// Categories of system-generated alerts surfaced on the dashboard.
/// </summary>
public enum AlertType
{
    CriticalStock = 1,   // part dropped below its minimum stock level
    MaintenanceDue = 2   // a vehicle's scheduled maintenance is approaching
}
