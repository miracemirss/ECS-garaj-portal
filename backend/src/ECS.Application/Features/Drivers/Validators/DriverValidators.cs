using ECS.Application.Features.Drivers.Dtos;
using FluentValidation;

namespace ECS.Application.Features.Drivers.Validators;

public sealed class CreateDriverRequestValidator : AbstractValidator<CreateDriverRequest>
{
    public CreateDriverRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).Matches(PhonePattern).When(x => !string.IsNullOrWhiteSpace(x.Phone))
            .WithMessage("Phone format is invalid.");
    }

    internal const string PhonePattern = @"^\+?[0-9\s\-()]{7,20}$";
}

public sealed class UpdateDriverRequestValidator : AbstractValidator<UpdateDriverRequest>
{
    public UpdateDriverRequestValidator()
    {
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).Matches(CreateDriverRequestValidator.PhonePattern).When(x => !string.IsNullOrWhiteSpace(x.Phone))
            .WithMessage("Phone format is invalid.");
    }
}
