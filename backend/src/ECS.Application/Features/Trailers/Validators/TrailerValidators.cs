using ECS.Application.Features.Trailers.Dtos;
using FluentValidation;

namespace ECS.Application.Features.Trailers.Validators;

public sealed class CreateTrailerRequestValidator : AbstractValidator<CreateTrailerRequest>
{
    public CreateTrailerRequestValidator()
    {
        RuleFor(x => x.PlateNo).NotEmpty().MaximumLength(16);
        RuleFor(x => x.CapacityKg).GreaterThanOrEqualTo(0).When(x => x.CapacityKg.HasValue);
    }
}

public sealed class UpdateTrailerRequestValidator : AbstractValidator<UpdateTrailerRequest>
{
    public UpdateTrailerRequestValidator()
    {
        RuleFor(x => x.CapacityKg).GreaterThanOrEqualTo(0).When(x => x.CapacityKg.HasValue);
    }
}
