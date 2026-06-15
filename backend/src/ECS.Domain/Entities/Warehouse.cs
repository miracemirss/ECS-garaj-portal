using ECS.Domain.Common;
using ECS.Domain.Exceptions;

namespace ECS.Domain.Entities;

/// <summary>A stock-keeping location.</summary>
public class Warehouse : AuditableEntity
{
    private Warehouse() { }

    private Warehouse(string code, string name)
    {
        Code = code;
        Name = name;
        IsActive = true;
    }

    public string Code { get; private set; } = null!;   // unique
    public string Name { get; private set; } = null!;
    public string? Address { get; private set; }
    public bool IsActive { get; private set; }

    public static Warehouse Create(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Warehouse code is required.");
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Warehouse name is required.");
        }
        return new Warehouse(code.Trim().ToUpperInvariant(), name.Trim());
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Warehouse name is required.");
        }
        Name = name.Trim();
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
