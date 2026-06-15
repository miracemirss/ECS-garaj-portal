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
    public string? Vin { get; private set; }               // chassis number, unique
    public string? TrailerType { get; private set; }       // 'Tent', 'Frigo', 'Lowbed', ...
    public string? Brand { get; private set; }
    public string? Model { get; private set; }
    public int? ModelYear { get; private set; }
    public TrailerStatus Status { get; private set; }
    public decimal? CapacityKg { get; private set; }
    public int? TireConditionPercent { get; private set; } // tire life 0-100
    public DateOnly? PurchaseDate { get; private set; }
    public DateOnly? InspectionDueDate { get; private set; }
    public DateOnly? InsuranceDueDate { get; private set; }

    public static Trailer Create(
        string plateNo, string? trailerType = null, string? brand = null,
        decimal? capacityKg = null, string? vin = null, int? tireConditionPercent = null)
    {
        if (capacityKg is < 0)
        {
            throw new DomainException("Capacity cannot be negative.");
        }
        if (tireConditionPercent is < 0 or > 100)
        {
            throw new DomainException("Tire condition must be between 0 and 100.");
        }

        return new Trailer(PlateNumber.Normalize(plateNo))
        {
            TrailerType = trailerType?.Trim(),
            Brand = brand?.Trim(),
            CapacityKg = capacityKg,
            Vin = string.IsNullOrWhiteSpace(vin) ? null : vin.Trim().ToUpperInvariant(),
            TireConditionPercent = tireConditionPercent
        };
    }

    public void UpdateDetails(string? trailerType, string? brand, string? model, decimal? capacityKg, int? tireConditionPercent)
    {
        if (capacityKg is < 0)
        {
            throw new DomainException("Capacity cannot be negative.");
        }
        if (tireConditionPercent is < 0 or > 100)
        {
            throw new DomainException("Tire condition must be between 0 and 100.");
        }
        TrailerType = trailerType?.Trim();
        Brand = brand?.Trim();
        Model = model?.Trim();
        CapacityKg = capacityKg;
        TireConditionPercent = tireConditionPercent;
    }

    public void ChangeStatus(TrailerStatus status) => Status = status;

    public void SetComplianceDates(DateOnly? inspectionDue, DateOnly? insuranceDue)
    {
        InspectionDueDate = inspectionDue;
        InsuranceDueDate = insuranceDue;
    }
}
