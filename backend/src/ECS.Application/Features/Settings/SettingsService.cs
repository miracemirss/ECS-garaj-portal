using ECS.Application.Common;
using ECS.Application.Common.Interfaces;
using ECS.Application.Features.Settings.Dtos;
using ECS.Domain.Entities;
using ECS.Domain.Exceptions;
using ECS.Shared.Results;
using FluentValidation;

namespace ECS.Application.Features.Settings;

public sealed class SettingsService : ISettingsService
{
    private readonly IRepository<CompanySetting> _settings;
    private readonly IUnitOfWork _uow;
    private readonly IAuditLogService _audit;
    private readonly IValidator<UpdateSettingsRequest> _validator;

    public SettingsService(
        IRepository<CompanySetting> settings,
        IUnitOfWork uow,
        IAuditLogService audit,
        IValidator<UpdateSettingsRequest> validator)
    {
        _settings = settings;
        _uow = uow;
        _audit = audit;
        _validator = validator;
    }

    public async Task<Result<CompanySettingDto>> GetAsync(CancellationToken ct = default)
    {
        var settings = await _settings.FirstOrDefaultAsync(_ => true, ct);
        return settings is null
            ? Result.Failure<CompanySettingDto>(Error.NotFound("Company settings have not been initialized."))
            : Result.Success(MapToDto(settings));
    }

    public async Task<Result<CompanySettingDto>> UpdateAsync(UpdateSettingsRequest request, CancellationToken ct = default)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result.Failure<CompanySettingDto>(Error.Validation(validation.ToMessage()));
        }

        var settings = await _settings.FirstOrDefaultAsync(_ => true, ct);
        if (settings is null)
        {
            return Result.Failure<CompanySettingDto>(Error.NotFound("Company settings have not been initialized."));
        }

        try
        {
            settings.UpdateProfile(request.CompanyName, request.DefaultCurrency);
            settings.UpdateThresholds(request.MaintenanceDueKmThreshold, request.MaintenanceDueDaysThreshold, request.DocumentExpiryDaysThreshold);
            settings.SetLowStockCheck(request.LowStockCheckEnabled);
        }
        catch (DomainException ex)
        {
            return Result.Failure<CompanySettingDto>(Error.Validation(ex.Message));
        }

        _settings.Update(settings);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("CompanySettingsUpdated", nameof(CompanySetting), settings.Id.ToString(), null, ct);

        return Result.Success(MapToDto(settings));
    }

    private static CompanySettingDto MapToDto(CompanySetting s) => new(
        s.Id, s.CompanyName, s.DefaultCurrency, s.MaintenanceDueKmThreshold,
        s.MaintenanceDueDaysThreshold, s.DocumentExpiryDaysThreshold, s.LowStockCheckEnabled);
}
