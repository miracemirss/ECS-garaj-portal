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
        RuleFor(x => x).Must(x => DateRangeIsValid(x.LicenseStartDate, x.LicenseExpiryDate))
            .WithMessage("Ehliyet bitis tarihi baslangic tarihinden once olamaz.");
        RuleFor(x => x).Must(x => DateRangeIsValid(x.SrcStartDate, x.SrcEndDate))
            .WithMessage("SRC bitis tarihi baslangic tarihinden once olamaz.");
        RuleFor(x => x).Must(x => DateRangeIsValid(x.PsychotechnicalStartDate, x.PsychotechnicalEndDate))
            .WithMessage("Psikoteknik bitis tarihi baslangic tarihinden once olamaz.");
        RuleFor(x => x).Must(x => DateRangeIsValid(x.VisaStartDate, x.VisaEndDate))
            .WithMessage("Vize bitis tarihi baslangic tarihinden once olamaz.");
        RuleFor(x => x).Must(x => DateRangeIsValid(x.PassportStartDate, x.PassportEndDate))
            .WithMessage("Pasaport bitis tarihi baslangic tarihinden once olamaz.");
    }

    internal const string PhonePattern = @"^\+?[0-9\s\-()]{7,20}$";
    internal static bool DateRangeIsValid(DateOnly? start, DateOnly? end) => !start.HasValue || !end.HasValue || end.Value >= start.Value;
}

public sealed class UpdateDriverRequestValidator : AbstractValidator<UpdateDriverRequest>
{
    public UpdateDriverRequestValidator()
    {
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).Matches(CreateDriverRequestValidator.PhonePattern).When(x => !string.IsNullOrWhiteSpace(x.Phone))
            .WithMessage("Phone format is invalid.");
        RuleFor(x => x).Must(x => CreateDriverRequestValidator.DateRangeIsValid(x.LicenseStartDate, x.LicenseExpiryDate))
            .WithMessage("Ehliyet bitis tarihi baslangic tarihinden once olamaz.");
        RuleFor(x => x).Must(x => CreateDriverRequestValidator.DateRangeIsValid(x.SrcStartDate, x.SrcEndDate))
            .WithMessage("SRC bitis tarihi baslangic tarihinden once olamaz.");
        RuleFor(x => x).Must(x => CreateDriverRequestValidator.DateRangeIsValid(x.PsychotechnicalStartDate, x.PsychotechnicalEndDate))
            .WithMessage("Psikoteknik bitis tarihi baslangic tarihinden once olamaz.");
        RuleFor(x => x).Must(x => CreateDriverRequestValidator.DateRangeIsValid(x.VisaStartDate, x.VisaEndDate))
            .WithMessage("Vize bitis tarihi baslangic tarihinden once olamaz.");
        RuleFor(x => x).Must(x => CreateDriverRequestValidator.DateRangeIsValid(x.PassportStartDate, x.PassportEndDate))
            .WithMessage("Pasaport bitis tarihi baslangic tarihinden once olamaz.");
    }
}
