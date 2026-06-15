using ECS.Application.Features.Maintenance.Dtos;
using ECS.Domain.Enums;
using FluentValidation;

namespace ECS.Application.Features.Maintenance.Validators;

public sealed class CreateWorkOrderRequestValidator : AbstractValidator<CreateWorkOrderRequest>
{
    public CreateWorkOrderRequestValidator()
    {
        RuleFor(x => x.TargetType).Must(t => Enum.TryParse<TargetType>(t, out _))
            .WithMessage("TargetType must be Vehicle or Trailer.");
        RuleFor(x => x.TargetId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.MaintenanceType).Must(t => Enum.TryParse<WorkOrderType>(t, out _))
            .WithMessage("Invalid maintenance type.");
        RuleFor(x => x.LaborCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OdometerBeforeKm).GreaterThanOrEqualTo(0).When(x => x.OdometerBeforeKm.HasValue);
    }
}

public sealed class UpdateWorkOrderRequestValidator : AbstractValidator<UpdateWorkOrderRequest>
{
    public UpdateWorkOrderRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LaborCost).GreaterThanOrEqualTo(0);
    }
}

public sealed class AddWorkOrderPartRequestValidator : AbstractValidator<AddWorkOrderPartRequest>
{
    public AddWorkOrderPartRequestValidator()
    {
        RuleFor(x => x.PartId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0).When(x => x.UnitCost.HasValue);
    }
}
