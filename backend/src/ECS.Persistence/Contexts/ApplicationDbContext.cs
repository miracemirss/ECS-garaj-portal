using ECS.Domain.Common;
using ECS.Domain.Entities;
using ECS.Domain.Enums;
using ECS.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECS.Persistence.Contexts;

/// <summary>
/// EF Core database context mapped to the existing PostgreSQL schema (managed by the
/// raw SQL migrations under /database). Names follow snake_case via the naming
/// convention configured in DI; PostgreSQL enum types are mapped natively. Computed
/// C# properties, generated columns, and audit/soft-delete columns that a given
/// table does not have are explicitly ignored.
/// </summary>
public class ApplicationDbContext : DbContext
{
    private static readonly PascalCaseNameTranslator EnumLabels = new();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Trailer> Trailers => Set<Trailer>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<MaintenanceWorkOrder> MaintenanceWorkOrders => Set<MaintenanceWorkOrder>();
    public DbSet<MaintenanceTask> MaintenanceTasks => Set<MaintenanceTask>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<WorkOrderPart> WorkOrderParts => Set<WorkOrderPart>();
    public DbSet<VehicleTrailerAssignment> VehicleTrailerAssignments => Set<VehicleTrailerAssignment>();
    public DbSet<DriverVehicleAssignment> DriverVehicleAssignments => Set<DriverVehicleAssignment>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<CompanySetting> CompanySettings => Set<CompanySetting>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<ReportFile> ReportFiles => Set<ReportFile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ----- PostgreSQL enum types (labels kept PascalCase to match the schema) -----
        modelBuilder.HasPostgresEnum<VehicleStatus>(null, "asset_status", EnumLabels);
        modelBuilder.HasPostgresEnum<TrailerStatus>(null, "asset_status", EnumLabels);
        modelBuilder.HasPostgresEnum<DriverStatus>(null, "driver_status", EnumLabels);
        modelBuilder.HasPostgresEnum<AssignmentStatus>(null, "assignment_status", EnumLabels);
        modelBuilder.HasPostgresEnum<WorkOrderStatus>(null, "work_order_status", EnumLabels);
        modelBuilder.HasPostgresEnum<TargetType>(null, "work_order_target_type", EnumLabels);
        modelBuilder.HasPostgresEnum<WorkOrderType>(null, "maintenance_type", EnumLabels);
        modelBuilder.HasPostgresEnum<StockMovementType>(null, "stock_movement_type", EnumLabels);
        modelBuilder.HasPostgresEnum<AlertType>(null, "alert_type", EnumLabels);
        modelBuilder.HasPostgresEnum<AlertPriority>(null, "alert_severity", EnumLabels);
        modelBuilder.HasPostgresEnum<AlertStatus>(null, "alert_status", EnumLabels);
        modelBuilder.HasPostgresEnum<DocumentOwnerType>(null, "document_owner_type", EnumLabels);
        modelBuilder.HasPostgresEnum<DocumentType>(null, "document_type", EnumLabels);

        // ----- Identity -----
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            // The schema stores a boolean is_active, not a user_status enum.
            e.Property(u => u.Status)
                .HasColumnName("is_active")
                .HasConversion(
                    status => status == UserStatus.Active,
                    active => active ? UserStatus.Active : UserStatus.Inactive);
            e.Ignore(u => u.IsActive);
            e.Ignore(u => u.DomainEvents);
            e.Ignore(u => u.RefreshTokens);
            e.HasMany(u => u.Roles)
                .WithMany()
                .UsingEntity(
                    "user_roles",
                    right => right.HasOne(typeof(Role)).WithMany().HasForeignKey("role_id"),
                    left => left.HasOne(typeof(User)).WithMany().HasForeignKey("user_id"));
        });

        modelBuilder.Entity<Role>(e =>
        {
            e.ToTable("roles");
            // roles only has created_at / updated_at.
            e.Ignore(r => r.CreatedBy);
            e.Ignore(r => r.UpdatedBy);
            IgnoreSoftDelete(e);
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens");
            e.Ignore(t => t.User);
            e.Ignore(t => t.CreatedByIp); // inet column; not needed by the app
        });

        // ----- Fleet -----
        modelBuilder.Entity<Vehicle>(e =>
        {
            e.ToTable("vehicles");
            e.Ignore(v => v.DomainEvents);
        });

        modelBuilder.Entity<Trailer>(e =>
        {
            e.ToTable("trailers");
            e.Ignore(t => t.DomainEvents);
        });

        modelBuilder.Entity<Driver>(e =>
        {
            e.ToTable("drivers");
            e.Ignore(d => d.FullName);
            e.Ignore(d => d.DomainEvents);
        });

        // ----- Inventory -----
        modelBuilder.Entity<Warehouse>(e =>
        {
            e.ToTable("warehouses");
            IgnoreSoftDelete(e);
        });

        modelBuilder.Entity<Supplier>(e =>
        {
            e.ToTable("suppliers");
            IgnoreSoftDelete(e);
        });

        modelBuilder.Entity<Part>(e =>
        {
            e.ToTable("parts");
            e.Ignore(p => p.IsBelowMinimum);
            e.Ignore(p => p.DomainEvents);
            e.Ignore(p => p.Warehouse);
            e.Ignore(p => p.Supplier);
        });

        // ----- Maintenance -----
        modelBuilder.Entity<MaintenanceWorkOrder>(e =>
        {
            e.ToTable("maintenance_work_orders");
            e.Ignore(w => w.TotalCost); // generated column / computed in C#
            e.Ignore(w => w.DomainEvents);
            e.Ignore(w => w.Vehicle);
            e.Ignore(w => w.Trailer);
            e.Ignore(w => w.Parts);
            e.Ignore(w => w.Tasks);
        });

        modelBuilder.Entity<MaintenanceTask>(e =>
        {
            e.ToTable("maintenance_tasks");
            IgnoreSoftDelete(e);
        });

        // ----- Stock -----
        modelBuilder.Entity<StockMovement>(e =>
        {
            e.ToTable("stock_movements");
            e.Ignore(m => m.Part);
        });

        modelBuilder.Entity<WorkOrderPart>(e =>
        {
            e.ToTable("work_order_parts");
            e.Ignore(p => p.LineTotal); // generated column / computed in C#
            e.Ignore(p => p.Part);
            e.Ignore(p => p.StockMovement);
        });

        // ----- Assignments -----
        modelBuilder.Entity<VehicleTrailerAssignment>(e =>
        {
            e.ToTable("vehicle_trailer_assignments");
            e.Ignore(a => a.DomainEvents);
            e.Ignore(a => a.Vehicle);
            e.Ignore(a => a.Trailer);
            IgnoreSoftDelete(e);
        });

        modelBuilder.Entity<DriverVehicleAssignment>(e =>
        {
            e.ToTable("driver_vehicle_assignments");
            e.Ignore(a => a.DomainEvents);
            e.Ignore(a => a.Driver);
            e.Ignore(a => a.Vehicle);
            IgnoreSoftDelete(e);
        });

        // ----- Operational -----
        modelBuilder.Entity<Alert>(e =>
        {
            e.ToTable("alerts");
            e.Property(a => a.Priority).HasColumnName("severity");
        });

        modelBuilder.Entity<CompanySetting>(e =>
        {
            e.ToTable("company_settings");
            // company_settings has created_at / updated_at / updated_by only.
            e.Ignore(c => c.CreatedBy);
            IgnoreSoftDelete(e);
        });

        modelBuilder.Entity<Document>(e => e.ToTable("documents"));

        modelBuilder.Entity<ReportFile>(e =>
        {
            e.ToTable("report_files");
            e.Property(r => r.Parameters).HasColumnType("jsonb");
        });
    }

    private static void IgnoreSoftDelete<TEntity>(EntityTypeBuilder<TEntity> e)
        where TEntity : AuditableEntity
    {
        e.Ignore(x => x.IsDeleted);
        e.Ignore(x => x.DeletedAt);
        e.Ignore(x => x.DeletedBy);
    }
}
