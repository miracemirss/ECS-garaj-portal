using ECS.Domain.Common;
using ECS.Domain.DomainEvents;
using ECS.Domain.Enums;
using ECS.Domain.Exceptions;

namespace ECS.Domain.Entities;

/// <summary>
/// A maintenance work order targeting EITHER a vehicle OR a trailer (never both).
/// Owns its tasks and consumed parts. parts_cost is derived from the part lines;
/// total_cost = labor + parts.
/// </summary>
public class MaintenanceWorkOrder : AggregateRoot
{
    private readonly List<WorkOrderPart> _parts = [];
    private readonly List<MaintenanceTask> _tasks = [];

    private MaintenanceWorkOrder() { }

    private MaintenanceWorkOrder(TargetType targetType, Guid? vehicleId, Guid? trailerId, string title, WorkOrderType type)
    {
        TargetType = targetType;
        VehicleId = vehicleId;
        TrailerId = trailerId;
        Title = title;
        MaintenanceType = type;
        Status = WorkOrderStatus.Open;
    }

    public string? WorkOrderNo { get; private set; }    // assigned on persistence (DB trigger / service)
    public TargetType TargetType { get; private set; }
    public Guid? VehicleId { get; private set; }
    public Guid? TrailerId { get; private set; }
    public WorkOrderType MaintenanceType { get; private set; }
    public WorkOrderStatus Status { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public int? OdometerBeforeKm { get; private set; }
    public int? OdometerAfterKm { get; private set; }
    public DateOnly? ScheduledDate { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public Guid? SupplierId { get; private set; }
    public Guid? AssignedTo { get; private set; }
    public decimal LaborCost { get; private set; }
    public decimal PartsCost { get; private set; }
    public decimal TotalCost => LaborCost + PartsCost;

    public Vehicle? Vehicle { get; private set; }
    public Trailer? Trailer { get; private set; }
    public IReadOnlyCollection<WorkOrderPart> Parts => _parts.AsReadOnly();
    public IReadOnlyCollection<MaintenanceTask> Tasks => _tasks.AsReadOnly();

    public static MaintenanceWorkOrder CreateForVehicle(Guid vehicleId, string title, WorkOrderType type, int? odometerBeforeKm = null)
    {
        ValidateTitle(title);
        if (odometerBeforeKm is < 0)
        {
            throw new DomainException("Odometer before cannot be negative.");
        }
        return new MaintenanceWorkOrder(TargetType.Vehicle, vehicleId, null, title.Trim(), type)
        {
            OdometerBeforeKm = odometerBeforeKm
        };
    }

    public static MaintenanceWorkOrder CreateForTrailer(Guid trailerId, string title, WorkOrderType type)
    {
        ValidateTitle(title);
        return new MaintenanceWorkOrder(TargetType.Trailer, null, trailerId, title.Trim(), type);
    }

    /// <summary>Sets the human-friendly number (DB trigger or service supplies it).</summary>
    public void AssignNumber(string workOrderNo)
    {
        if (string.IsNullOrWhiteSpace(workOrderNo))
        {
            throw new DomainException("Work order number is required.");
        }
        WorkOrderNo = workOrderNo;
    }

    public void SetLaborCost(decimal laborCost)
    {
        if (laborCost < 0)
        {
            throw new DomainException("Labor cost cannot be negative.");
        }
        LaborCost = laborCost;
    }

    public void Schedule(DateOnly date) => ScheduledDate = date;
    public void AssignTechnician(Guid? userId) => AssignedTo = userId;
    public void SetSupplier(Guid? supplierId) => SupplierId = supplierId;
    public void SetDescription(string? description) => Description = description;

    public void UpdateDetails(string title, string? description)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Work order title is required.");
        }
        if (Status is WorkOrderStatus.Completed or WorkOrderStatus.Cancelled)
        {
            throw new InvalidWorkOrderStateException($"Cannot edit a {Status} work order.");
        }
        Title = title.Trim();
        Description = description;
    }

    public void Start(DateTime utcNow)
    {
        if (Status is not (WorkOrderStatus.Draft or WorkOrderStatus.Open))
        {
            throw new InvalidWorkOrderStateException($"Cannot start a work order in status {Status}.");
        }
        Status = WorkOrderStatus.InProgress;
        StartedAt = utcNow;
    }

    /// <summary>
    /// Adds a consumed-part line. The corresponding stock decrement + StockMovement
    /// is orchestrated by the Application service; the movement id is linked here.
    /// </summary>
    public WorkOrderPart AddPart(Guid partId, decimal quantity, decimal unitCost, Guid? stockMovementId = null)
    {
        if (Status is WorkOrderStatus.Completed or WorkOrderStatus.Cancelled)
        {
            throw new InvalidWorkOrderStateException($"Cannot add parts to a {Status} work order.");
        }

        var line = WorkOrderPart.Create(Id, partId, quantity, unitCost, stockMovementId);
        _parts.Add(line);
        PartsCost += line.LineTotal;   // incremental; correct without loading the whole collection
        return line;
    }

    public MaintenanceTask AddTask(string description, decimal? laborHours = null)
    {
        var task = MaintenanceTask.Create(Id, description, laborHours, _tasks.Count);
        _tasks.Add(task);
        return task;
    }

    public void Complete(int? odometerAfterKm, DateTime utcNow)
    {
        if (Status is not (WorkOrderStatus.Open or WorkOrderStatus.InProgress))
        {
            throw new InvalidWorkOrderStateException($"Cannot complete a work order in status {Status}.");
        }
        if (odometerAfterKm is not null && OdometerBeforeKm is not null && odometerAfterKm < OdometerBeforeKm)
        {
            throw new DomainException("Odometer after cannot be less than odometer before.");
        }

        Status = WorkOrderStatus.Completed;
        CompletedAt = utcNow;
        if (odometerAfterKm is not null)
        {
            OdometerAfterKm = odometerAfterKm;
        }

        RaiseDomainEvent(new WorkOrderCompletedEvent(Id, TargetType, VehicleId, TrailerId, OdometerAfterKm));
    }

    public void Cancel()
    {
        if (Status == WorkOrderStatus.Completed)
        {
            throw new InvalidWorkOrderStateException("A completed work order cannot be cancelled.");
        }
        Status = WorkOrderStatus.Cancelled;
    }

    /// <summary>Recomputes parts cost from the loaded lines (use when the full collection is loaded).</summary>
    public void RecalculatePartsCost() => PartsCost = _parts.Sum(p => p.LineTotal);

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Work order title is required.");
        }
    }
}
