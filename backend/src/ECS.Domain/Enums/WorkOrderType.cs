namespace ECS.Domain.Enums;

/// <summary>Category of maintenance performed by a work order.</summary>
public enum WorkOrderType
{
    Preventive = 1,
    Corrective = 2,
    Inspection = 3,
    Tire = 4,
    Other = 5
}
