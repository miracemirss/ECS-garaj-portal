namespace ECS.Shared.Constants;

/// <summary>
/// Application role names. Used by both the API ([Authorize(Roles = ...)]) and
/// the JWT issuer. Kept here so a single source of truth is shared across layers.
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string FleetManager = "FleetManager";
    public const string Technician = "Technician";
    public const string WarehouseManager = "WarehouseManager";
    public const string Viewer = "Viewer";
}
