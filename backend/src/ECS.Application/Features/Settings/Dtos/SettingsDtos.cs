namespace ECS.Application.Features.Settings.Dtos;

public sealed record CompanySettingDto(
    Guid Id,
    string CompanyName,
    string DefaultCurrency,
    int MaintenanceDueKmThreshold,
    int MaintenanceDueDaysThreshold,
    int DocumentExpiryDaysThreshold,
    bool LowStockCheckEnabled);

public sealed record UpdateSettingsRequest(
    string CompanyName,
    string DefaultCurrency,
    int MaintenanceDueKmThreshold,
    int MaintenanceDueDaysThreshold,
    int DocumentExpiryDaysThreshold,
    bool LowStockCheckEnabled);
