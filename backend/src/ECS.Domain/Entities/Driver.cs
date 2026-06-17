using ECS.Domain.Common;
using ECS.Domain.Enums;
using ECS.Domain.Exceptions;

namespace ECS.Domain.Entities;

/// <summary>A driver who can be assigned to a vehicle (history-tracked).</summary>
public class Driver : AggregateRoot
{
    private Driver() { }

    private Driver(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
        Status = DriverStatus.Active;
    }

    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string FullName => $"{FirstName} {LastName}";
    public string? NationalId { get; private set; }    // unique when present
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? LicenseNo { get; private set; }
    public string? LicenseClass { get; private set; }
    public DateOnly? LicenseStartDate { get; private set; }
    public DateOnly? LicenseExpiryDate { get; private set; }
    public DateOnly? SrcStartDate { get; private set; }
    public DateOnly? SrcEndDate { get; private set; }
    public DateOnly? PsychotechnicalStartDate { get; private set; }
    public DateOnly? PsychotechnicalEndDate { get; private set; }
    public DateOnly? VisaStartDate { get; private set; }
    public DateOnly? VisaEndDate { get; private set; }
    public DateOnly? PassportStartDate { get; private set; }
    public DateOnly? PassportEndDate { get; private set; }
    public string? DocumentNote { get; private set; }
    public DriverStatus Status { get; private set; }
    public DateOnly? HireDate { get; private set; }
    public DateOnly? BirthDate { get; private set; }

    public static Driver Create(string firstName, string lastName, string? nationalId = null)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            throw new DomainException("Driver first and last name are required.");
        }

        return new Driver(firstName.Trim(), lastName.Trim())
        {
            NationalId = string.IsNullOrWhiteSpace(nationalId) ? null : nationalId.Trim()
        };
    }

    public void ChangeStatus(DriverStatus status) => Status = status;

    public void SetLicense(string? licenseNo, string? licenseClass, DateOnly? start, DateOnly? expiry)
    {
        LicenseNo = licenseNo?.Trim();
        LicenseClass = licenseClass?.Trim();
        LicenseStartDate = start;
        LicenseExpiryDate = expiry;
    }

    public void SetDocuments(
        DateOnly? srcStartDate,
        DateOnly? srcEndDate,
        DateOnly? psychotechnicalStartDate,
        DateOnly? psychotechnicalEndDate,
        DateOnly? visaStartDate,
        DateOnly? visaEndDate,
        DateOnly? passportStartDate,
        DateOnly? passportEndDate,
        string? documentNote)
    {
        SrcStartDate = srcStartDate;
        SrcEndDate = srcEndDate;
        PsychotechnicalStartDate = psychotechnicalStartDate;
        PsychotechnicalEndDate = psychotechnicalEndDate;
        VisaStartDate = visaStartDate;
        VisaEndDate = visaEndDate;
        PassportStartDate = passportStartDate;
        PassportEndDate = passportEndDate;
        DocumentNote = string.IsNullOrWhiteSpace(documentNote) ? null : documentNote.Trim();
    }

    public void SetContact(string? phone, string? email)
    {
        Phone = phone?.Trim();
        Email = email?.Trim();
    }
}
