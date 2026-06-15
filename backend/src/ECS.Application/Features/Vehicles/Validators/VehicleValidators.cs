using ECS.Application.Features.Vehicles.Dtos;
using FluentValidation;

namespace ECS.Application.Features.Vehicles.Validators;

public sealed class CreateVehicleRequestValidator : AbstractValidator<CreateVehicleRequest>
{
    public CreateVehicleRequestValidator()
    {
        RuleFor(x => x.PlateNo).NotEmpty().MaximumLength(16);
        RuleFor(x => x.Brand).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ModelYear).InclusiveBetween(1950, 2100).When(x => x.ModelYear.HasValue);
    }
}

public sealed class UpdateVehicleRequestValidator : AbstractValidator<UpdateVehicleRequest>
{
    public UpdateVehicleRequestValidator()
    {
        RuleFor(x => x.Brand).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ModelYear).InclusiveBetween(1950, 2100).When(x => x.ModelYear.HasValue);
        RuleFor(x => x.MaintenanceIntervalKm).GreaterThan(0).When(x => x.MaintenanceIntervalKm.HasValue);
        RuleFor(x => x.MaintenanceIntervalDays).GreaterThan(0).When(x => x.MaintenanceIntervalDays.HasValue);
    }
}
