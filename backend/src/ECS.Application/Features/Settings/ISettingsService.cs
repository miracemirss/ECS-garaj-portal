using ECS.Application.Features.Settings.Dtos;
using ECS.Shared.Results;

namespace ECS.Application.Features.Settings;

public interface ISettingsService
{
    Task<Result<CompanySettingDto>> GetAsync(CancellationToken ct = default);
    Task<Result<CompanySettingDto>> UpdateAsync(UpdateSettingsRequest request, CancellationToken ct = default);
}
