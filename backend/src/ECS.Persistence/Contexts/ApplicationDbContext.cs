using System.Reflection;
using System.Text;
using ECS.Domain.Common;
using ECS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECS.Persistence.Contexts;

/// <summary>
/// EF Core database context. Maps the ECS.Domain entities onto the hand-written
/// PostgreSQL schema (database/migrations/*.sql). Per-entity configurations live
/// in ECS.Persistence.Configurations; this class wires them up, converts
/// table/column names to snake_case, and detaches domain-event collections.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Trailer> Trailers => Set<Trailer>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<MaintenanceWorkOrder> MaintenanceWorkOrders => Set<MaintenanceWorkOrder>();
    public DbSet<MaintenanceTask> MaintenanceTasks => Set<MaintenanceTask>();
    public DbSet<WorkOrderPart> WorkOrderParts => Set<WorkOrderPart>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<CompanySetting> CompanySettings => Set<CompanySetting>();
    public DbSet<ReportFile> ReportFiles => Set<ReportFile>();
    public DbSet<VehicleTrailerAssignment> VehicleTrailerAssignments => Set<VehicleTrailerAssignment>();
    public DbSet<DriverVehicleAssignment> DriverVehicleAssignments => Set<DriverVehicleAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Domain-event collections are never persisted; detach them before validation.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            if (typeof(IHasDomainEvents).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).Ignore(nameof(IHasDomainEvents.DomainEvents));
            }
        }

        // snake_case every column (table names are set explicitly in configurations).
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            foreach (var property in entityType.GetProperties().ToList())
            {
                var current = property.GetColumnName();
                if (!string.IsNullOrEmpty(current))
                {
                    property.SetColumnName(ToSnakeCase(current));
                }
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>Converts a PascalCase/camelCase identifier to snake_case (already-snake names pass through).</summary>
    internal static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var builder = new StringBuilder(input.Length + 8);
        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];
            if (char.IsUpper(c))
            {
                if (i > 0 && (char.IsLower(input[i - 1]) || char.IsDigit(input[i - 1])))
                {
                    builder.Append('_');
                }
                builder.Append(char.ToLowerInvariant(c));
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }
}
