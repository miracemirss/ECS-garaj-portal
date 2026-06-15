using ECS.Domain.Common;
using ECS.Domain.Exceptions;

namespace ECS.Domain.Entities;

/// <summary>A checklist item within a maintenance work order.</summary>
public class MaintenanceTask : AuditableEntity
{
    private MaintenanceTask() { }

    private MaintenanceTask(Guid workOrderId, string description, decimal? laborHours, int sortOrder)
    {
        WorkOrderId = workOrderId;
        Description = description;
        LaborHours = laborHours;
        SortOrder = sortOrder;
    }

    public Guid WorkOrderId { get; private set; }
    public string Description { get; private set; } = null!;
    public bool IsCompleted { get; private set; }
    public decimal? LaborHours { get; private set; }
    public int SortOrder { get; private set; }

    public static MaintenanceTask Create(Guid workOrderId, string description, decimal? laborHours, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("Task description is required.");
        }
        if (laborHours is < 0)
        {
            throw new DomainException("Labor hours cannot be negative.");
        }
        return new MaintenanceTask(workOrderId, description.Trim(), laborHours, sortOrder);
    }

    public void Complete() => IsCompleted = true;
    public void Reopen() => IsCompleted = false;
}
