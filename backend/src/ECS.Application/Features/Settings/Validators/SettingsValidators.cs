using ECS.Application.Features.Settings.Dtos;
using FluentValidation;

namespace ECS.Application.Features.Settings.Validators;

public sealed class UpdateSettingsRequestValidator : AbstractValidator<UpdateSettingsRequest>
{
    public UpdateSettingsRequestValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DefaultCurrency).NotEmpty().Length(3);
        RuleFor(x => x.MaintenanceDueKmThreshold).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaintenanceDueDaysThreshold).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DocumentExpiryDaysThreshold).GreaterThanOrEqualTo(0);
    }
}
