using ECS.Application.Features.Trailers.Dtos;
using ECS.Application.Common;
using FluentValidation;

namespace ECS.Application.Features.Trailers.Validators;

public sealed class CreateTrailerRequestValidator : AbstractValidator<CreateTrailerRequest>
{
    public CreateTrailerRequestValidator()
    {
        RuleFor(x => x.PlateNo).NotEmpty().MaximumLength(16);
        RuleFor(x => x.TrailerType)
            .Must(FleetOptions.IsValidTrailerType)
            .WithMessage("Gecerli bir dorse turu secin.");
        RuleFor(x => x.CapacityKg).GreaterThanOrEqualTo(0).When(x => x.CapacityKg.HasValue);
        RuleFor(x => x.TireConditionPercent).InclusiveBetween(0, 100).When(x => x.TireConditionPercent.HasValue);
    }
}

public sealed class UpdateTrailerRequestValidator : AbstractValidator<UpdateTrailerRequest>
{
    public UpdateTrailerRequestValidator()
    {
        RuleFor(x => x.CapacityKg).GreaterThanOrEqualTo(0).When(x => x.CapacityKg.HasValue);
        RuleFor(x => x.TrailerType)
            .Must(FleetOptions.IsValidTrailerType)
            .WithMessage("Gecerli bir dorse turu secin.");
        RuleFor(x => x.TireConditionPercent).InclusiveBetween(0, 100).When(x => x.TireConditionPercent.HasValue);
    }
}
