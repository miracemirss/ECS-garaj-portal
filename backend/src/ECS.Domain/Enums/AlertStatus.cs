namespace ECS.Domain.Enums;

/// <summary>Lifecycle of an alert.</summary>
public enum AlertStatus
{
    Open = 1,
    Acknowledged = 2,
    Resolved = 3,
    Dismissed = 4
}
