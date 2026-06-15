using ECS.Domain.Common;
using ECS.Domain.Exceptions;

namespace ECS.Domain.Entities;

/// <summary>Singleton company configuration (thresholds for alerts, currency, ...).</summary>
public class CompanySetting : AuditableEntity
{
    private CompanySetting() { }

    private CompanySetting(string companyName, string currency)
    {
        CompanyName = companyName;
        DefaultCurrency = currency;
    }

    public string CompanyName { get; private set; } = null!;
    public string DefaultCurrency { get; private set; } = "TRY";
    public int MaintenanceDueKmThreshold { get; private set; } = 1000;
    public int MaintenanceDueDaysThreshold { get; private set; } = 14;
    public int DocumentExpiryDaysThreshold { get; private set; } = 30;
    public bool LowStockCheckEnabled { get; private set; } = true;

    public static CompanySetting Create(string companyName, string currency = "TRY")
    {
        if (string.IsNullOrWhiteSpace(companyName))
        {
            throw new DomainException("Company name is required.");
        }
        return new CompanySetting(companyName.Trim(), string.IsNullOrWhiteSpace(currency) ? "TRY" : currency.ToUpperInvariant());
    }

    public void UpdateProfile(string companyName, string currency)
    {
        if (string.IsNullOrWhiteSpace(companyName))
        {
            throw new DomainException("Company name is required.");
        }
        CompanyName = companyName.Trim();
        DefaultCurrency = string.IsNullOrWhiteSpace(currency) ? DefaultCurrency : currency.ToUpperInvariant();
    }

    public void UpdateThresholds(int maintenanceKm, int maintenanceDays, int documentDays)
    {
        if (maintenanceKm < 0 || maintenanceDays < 0 || documentDays < 0)
        {
            throw new DomainException("Thresholds cannot be negative.");
        }
        MaintenanceDueKmThreshold = maintenanceKm;
        MaintenanceDueDaysThreshold = maintenanceDays;
        DocumentExpiryDaysThreshold = documentDays;
    }

    public void SetLowStockCheck(bool enabled) => LowStockCheckEnabled = enabled;
}
