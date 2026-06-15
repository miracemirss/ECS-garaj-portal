using ECS.Domain.Common;
using ECS.Domain.Exceptions;

namespace ECS.Domain.Entities;

/// <summary>A parts supplier / external service provider.</summary>
public class Supplier : AuditableEntity
{
    private Supplier() { }

    private Supplier(string name)
    {
        Name = name;
        IsActive = true;
    }

    public string Name { get; private set; } = null!;
    public string? TaxNo { get; private set; }   // unique when present
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public bool IsActive { get; private set; }

    public static Supplier Create(string name, string? taxNo = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Supplier name is required.");
        }
        return new Supplier(name.Trim())
        {
            TaxNo = string.IsNullOrWhiteSpace(taxNo) ? null : taxNo.Trim()
        };
    }

    public void SetContact(string? phone, string? email, string? address)
    {
        Phone = phone?.Trim();
        Email = email?.Trim();
        Address = address?.Trim();
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
