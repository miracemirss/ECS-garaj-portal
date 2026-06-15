using ECS.Domain.Common;
using ECS.Domain.Exceptions;

namespace ECS.Domain.Entities;

/// <summary>A security role (e.g. Admin, FleetManager). Built-in roles are flagged IsSystem.</summary>
public class Role : AuditableEntity
{
    private Role() { }

    private Role(string name, string? description, bool isSystem)
    {
        Name = name;
        Description = description;
        IsSystem = isSystem;
    }

    public string Name { get; private set; } = null!;   // unique
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; }

    public static Role Create(string name, string? description = null, bool isSystem = false)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Role name is required.");
        }
        return new Role(name.Trim(), description?.Trim(), isSystem);
    }

    public void Rename(string name)
    {
        if (IsSystem)
        {
            throw new DomainException("System roles cannot be renamed.");
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Role name is required.");
        }
        Name = name.Trim();
    }
}
