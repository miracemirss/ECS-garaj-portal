using ECS.Domain.Common;
using ECS.Domain.Enums;
using ECS.Domain.Exceptions;
using ECS.Domain.ValueObjects;

namespace ECS.Domain.Entities;

/// <summary>A trailer/semi-trailer. Separate aggregate from <see cref="Vehicle"/>.</summary>
public class Trailer : AggregateRoot
{
    private Trailer() { }

    private Trailer(string plateNo)
    {
        PlateNo = plateNo;
        Status = TrailerStatus.Active;
    }

    public string PlateNo { get; private set; } = null!;   // unique, normalized
    public string? TrailerType { get; private set; }       // 'Tent', 'Frigo', 'Lowbed', ...
    public string? Brand { get; private set; }
    public string? Model { get; private set; }
    public int? ModelYear { get; private set; }
    public TrailerStatus Status { get; private set; }
    public decimal? CapacityKg { get; private set; }
    public DateOnly? PurchaseDate { get; private set; }
    public DateOnly? InspectionDueDate { get; private set; }
    public DateOnly? InsuranceDueDate { get; private set; }

    public static Trailer Create(string plateNo, string? trailerType = null, string? brand = null, decimal? capacityKg = null)
    {
        if (capacityKg is < 0)
        {
            throw new DomainException("Capacity cannot be negative.");
        }

        return new Trailer(PlateNumber.Normalize(plateNo))
        {
            TrailerType = trailerType?.Trim(),
            Brand = brand?.Trim(),
            CapacityKg = capacityKg
        };
    }

    public void UpdateDetails(string? trailerType, string? brand, string? model, decimal? capacityKg)
    {
        if (capacityKg is < 0)
        {
            throw new DomainException("Capacity cannot be negative.");
        }
        TrailerType = trailerType?.Trim();
        Brand = brand?.Trim();
        Model = model?.Trim();
        CapacityKg = capacityKg;
    }

    public void ChangeStatus(TrailerStatus status) => Status = status;

    public void SetComplianceDates(DateOnly? inspectionDue, DateOnly? insuranceDue)
    {
        InspectionDueDate = inspectionDue;
        InsuranceDueDate = insuranceDue;
    }
}
