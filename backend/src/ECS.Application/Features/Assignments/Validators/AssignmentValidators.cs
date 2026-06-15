using ECS.Application.Features.Assignments.Dtos;
using FluentValidation;

namespace ECS.Application.Features.Assignments.Validators;

public sealed class AssignVehicleTrailerRequestValidator : AbstractValidator<AssignVehicleTrailerRequest>
{
    public AssignVehicleTrailerRequestValidator()
    {
        RuleFor(x => x.VehicleId).NotEmpty();
        RuleFor(x => x.TrailerId).NotEmpty();
    }
}

public sealed class AssignDriverVehicleRequestValidator : AbstractValidator<AssignDriverVehicleRequest>
{
    public AssignDriverVehicleRequestValidator()
    {
        RuleFor(x => x.DriverId).NotEmpty();
        RuleFor(x => x.VehicleId).NotEmpty();
    }
}
