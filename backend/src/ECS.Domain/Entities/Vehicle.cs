using ECS.Domain.Common;
using ECS.Domain.Enums;
using ECS.Domain.Exceptions;
using ECS.Domain.ValueObjects;

namespace ECS.Domain.Entities;

/// <summary>A fleet vehicle (tractor unit). Separate aggregate from <see cref="Trailer"/>.</summary>
public class Vehicle : AggregateRoot
{
    private Vehicle() { } // ORM materialization

    private Vehicle(string plateNo, string brand, string? model, int? modelYear)
    {
        PlateNo = plateNo;
        Brand = brand;
        Model = model;
        ModelYear = modelYear;
        Status = VehicleStatus.Active;
    }

    public string PlateNo { get; private set; } = null!;   // unique, normalized
    public string? Vin { get; private set; }               // chassis number, unique
    public string Brand { get; private set; } = null!;
    public string? Model { get; private set; }
    public int? ModelYear { get; private set; }
    public string? Color { get; private set; }
    public VehicleStatus Status { get; private set; }
    public int CurrentOdometerKm { get; private set; }
    public DateOnly? PurchaseDate { get; private set; }

    public int? MaintenanceIntervalKm { get; private set; }
    public int? MaintenanceIntervalDays { get; private set; }
    public DateOnly? LastMaintenanceDate { get; private set; }
    public int? LastMaintenanceOdometerKm { get; private set; }
    public int? NextMaintenanceKm { get; private set; }
    public DateOnly? NextMaintenanceDate { get; private set; }
    public DateOnly? InspectionDueDate { get; private set; }
    public DateOnly? InsuranceDueDate { get; private set; }

    public static Vehicle Create(string plateNo, string brand, string? model = null, int? modelYear = null, string? vin = null)
    {
        if (string.IsNullOrWhiteSpace(brand))
        {
            throw new DomainException("Vehicle brand is required.");
        }
        if (modelYear is < 1950 or > 2100)
        {
            throw new DomainException("Vehicle model year is out of range.");
        }

        return new Vehicle(PlateNumber.Normalize(plateNo), brand.Trim(), model?.Trim(), modelYear)
        {
            Vin = NormalizeVin(vin)
        };
    }

    /// <summary>
    /// Şasi (VIN) numarası zorunlu değildir. Boş/null ise null'a normalize edilir;
    /// girildiyse trim + büyük harfe çevrilir ve uzunluğu doğrulanır. Geçersiz ise
    /// <see cref="DomainException"/> fırlatılır (API katmanında 400'e maplenir, asla 500).
    /// </summary>
    private static string? NormalizeVin(string? vin)
    {
        if (string.IsNullOrWhiteSpace(vin))
        {
            return null;
        }

        var normalized = vin.Trim().ToUpperInvariant();
        if (normalized.Length < 11 || normalized.Length > 17)
        {
            throw new DomainException(
                "Şasi numarası geçersiz. Şasi numarası boş bırakılabilir veya geçerli formatta girilmelidir.");
        }

        return normalized;
    }

    public void UpdateOdometer(int km)
    {
        if (km < 0)
        {
            throw new DomainException("Odometer cannot be negative.");
        }
        if (km < CurrentOdometerKm)
        {
            throw new DomainException("Odometer cannot move backwards.");
        }
        CurrentOdometerKm = km;
    }

    public void SetMaintenancePlan(int? intervalKm, int? intervalDays)
    {
        if (intervalKm is <= 0)
        {
            throw new DomainException("Maintenance interval km must be positive.");
        }
        if (intervalDays is <= 0)
        {
            throw new DomainException("Maintenance interval days must be positive.");
        }
        MaintenanceIntervalKm = intervalKm;
        MaintenanceIntervalDays = intervalDays;
    }

    /// <summary>Rolls the odometer forward and reschedules the next maintenance after a completed work order.</summary>
    public void RecordMaintenanceCompletion(int odometerAfterKm, DateOnly today)
    {
        if (odometerAfterKm < 0)
        {
            throw new DomainException("Odometer cannot be negative.");
        }
        CurrentOdometerKm = Math.Max(CurrentOdometerKm, odometerAfterKm);
        LastMaintenanceOdometerKm = odometerAfterKm;
        LastMaintenanceDate = today;
        NextMaintenanceKm = MaintenanceIntervalKm.HasValue ? odometerAfterKm + MaintenanceIntervalKm.Value : null;
        NextMaintenanceDate = MaintenanceIntervalDays.HasValue ? today.AddDays(MaintenanceIntervalDays.Value) : null;
    }

    public void UpdateDetails(string brand, string? model, int? modelYear, string? color)
    {
        if (string.IsNullOrWhiteSpace(brand))
        {
            throw new DomainException("Vehicle brand is required.");
        }
        if (modelYear is < 1950 or > 2100)
        {
            throw new DomainException("Vehicle model year is out of range.");
        }
        Brand = brand.Trim();
        Model = model?.Trim();
        ModelYear = modelYear;
        Color = color?.Trim();
    }

    public void ChangeStatus(VehicleStatus status) => Status = status;

    public void SetComplianceDates(DateOnly? inspectionDue, DateOnly? insuranceDue)
    {
        InspectionDueDate = inspectionDue;
        InsuranceDueDate = insuranceDue;
    }
}
