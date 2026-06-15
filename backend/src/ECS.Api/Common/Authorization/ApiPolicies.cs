namespace ECS.Api.Common.Authorization;

/// <summary>Authorization policy names used by controllers.</summary>
public static class ApiPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string ManageFleet = "ManageFleet";            // vehicles, trailers, drivers, assignments
    public const string ManageMaintenance = "ManageMaintenance"; // work orders
    public const string ManageInventory = "ManageInventory";     // parts, stock
    public const string ViewReports = "ViewReports";             // any authenticated user
}
