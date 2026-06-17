using ECS.Domain.Entities;
using ECS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECS.Persistence.Configurations;

// ============================================================================
// EF Core entity configurations mapping the ECS.Domain entities onto the
// hand-written PostgreSQL schema in database/migrations/*.sql.
//
// Naming: table & column names are converted to snake_case globally in
// ApplicationDbContext.OnModelCreating, so configurations below only set
// names that DIVERGE from the snake_case-of-property-name default
// (e.g. Alert.Priority -> "severity", User.Status -> "is_active").
//
// Native PostgreSQL enum types (asset_status, stock_movement_type, ...) are
// registered on the NpgsqlDataSource in ECS.Persistence.DependencyInjection;
// EF maps each enum property through that data-source mapping.
// ============================================================================

internal static class ConfigurationHelpers
{
    /// <summary>
    /// AuditableEntity exposes CreatedBy/UpdatedBy and soft-delete columns, but
    /// some tables do not have all of them. Ignore the ones a given table lacks
    /// so EF never emits a non-existent column.
    /// </summary>
    public static void IgnoreSoftDelete<T>(this EntityTypeBuilder<T> b) where T : class
    {
        b.Ignore("IsDeleted");
        b.Ignore("DeletedAt");
        b.Ignore("DeletedBy");
    }
}

public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> b)
    {
        b.ToTable("vehicles");
        b.HasKey(v => v.Id);
        b.Property(v => v.PlateNo).HasColumnType("citext");
        b.HasQueryFilter(v => !v.IsDeleted);
    }
}

public sealed class TrailerConfiguration : IEntityTypeConfiguration<Trailer>
{
    public void Configure(EntityTypeBuilder<Trailer> b)
    {
        b.ToTable("trailers");
        b.HasKey(t => t.Id);
        b.Property(t => t.PlateNo).HasColumnType("citext");
        b.HasQueryFilter(t => !t.IsDeleted);
    }
}

public sealed class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> b)
    {
        b.ToTable("drivers");
        b.HasKey(d => d.Id);
        b.Ignore(d => d.FullName);
        b.Property(d => d.Email).HasColumnType("citext");
        b.HasQueryFilter(d => !d.IsDeleted);
    }
}

public sealed class PartConfiguration : IEntityTypeConfiguration<Part>
{
    public void Configure(EntityTypeBuilder<Part> b)
    {
        b.ToTable("parts");
        b.HasKey(p => p.Id);
        b.Ignore(p => p.IsBelowMinimum);
        b.Ignore(p => p.Warehouse);
        b.Ignore(p => p.Supplier);

        // quantity_in_stock is owned by the database: the prevent_negative_stock
        // trigger applies every change when a stock_movements row is inserted, and
        // fn_guard_part_stock rejects any direct write. So EF must never write it on
        // INSERT/UPDATE (read-only for writes) — the trigger is the single source of
        // truth. EF still reads the column on SELECT.
        var qty = b.Property(p => p.QuantityInStock).HasColumnType("numeric(14,3)");
        qty.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
        qty.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        b.HasQueryFilter(p => !p.IsDeleted);
    }
}

public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> b)
    {
        b.ToTable("stock_movements");
        b.HasKey(m => m.Id);
        b.Ignore(m => m.Part);
    }
}

public sealed class MaintenanceWorkOrderConfiguration : IEntityTypeConfiguration<MaintenanceWorkOrder>
{
    public void Configure(EntityTypeBuilder<MaintenanceWorkOrder> b)
    {
        b.ToTable("maintenance_work_orders");
        b.HasKey(w => w.Id);
        b.Ignore(w => w.TotalCost);      // total_cost is a GENERATED column
        b.Ignore(w => w.Vehicle);
        b.Ignore(w => w.Trailer);
        b.Ignore(w => w.Parts);
        b.Ignore(w => w.Tasks);
        b.HasQueryFilter(w => !w.IsDeleted);
    }
}

public sealed class MaintenanceTaskConfiguration : IEntityTypeConfiguration<MaintenanceTask>
{
    public void Configure(EntityTypeBuilder<MaintenanceTask> b)
    {
        b.ToTable("maintenance_tasks");
        b.HasKey(t => t.Id);
        b.IgnoreSoftDelete();            // maintenance_tasks has no soft-delete columns
    }
}

public sealed class WorkOrderPartConfiguration : IEntityTypeConfiguration<WorkOrderPart>
{
    public void Configure(EntityTypeBuilder<WorkOrderPart> b)
    {
        b.ToTable("work_order_parts");
        b.HasKey(p => p.Id);
        b.Ignore(p => p.LineTotal);      // line_total is a GENERATED column
        b.Ignore(p => p.Part);
        b.Ignore(p => p.StockMovement);
    }
}

public sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> b)
    {
        b.ToTable("warehouses");
        b.HasKey(w => w.Id);
        b.IgnoreSoftDelete();
    }
}

public sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> b)
    {
        b.ToTable("suppliers");
        b.HasKey(s => s.Id);
        b.Property(s => s.Email).HasColumnType("citext");
        b.IgnoreSoftDelete();
    }
}

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(u => u.Id);
        b.Property(u => u.Email).HasColumnType("citext");
        b.Ignore(u => u.IsActive);
        b.Ignore(u => u.RefreshTokens);

        // The users table has no status column — only is_active (boolean). Map the
        // domain Status enum onto is_active via a value converter (Active <-> true).
        b.Property(u => u.Status)
            .HasColumnName("is_active")
            .HasConversion(
                status => status == UserStatus.Active,
                active => active ? UserStatus.Active : UserStatus.Inactive);

        // Many-to-many with Role through the user_roles join table.
        b.HasMany(u => u.Roles)
            .WithMany()
            .UsingEntity(
                "user_roles",
                right => right.HasOne(typeof(Role)).WithMany().HasForeignKey("role_id"),
                left => left.HasOne(typeof(User)).WithMany().HasForeignKey("user_id"),
                join => join.HasKey("user_id", "role_id"));

        b.Navigation(u => u.Roles).UsePropertyAccessMode(PropertyAccessMode.Field);

        // UserRepository already filters !IsDeleted explicitly; no global query
        // filter here so it doesn't complicate the user_roles many-to-many.
    }
}

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("roles");
        b.HasKey(r => r.Id);
        b.Ignore("CreatedBy");           // roles table has no created_by / updated_by
        b.Ignore("UpdatedBy");
        b.IgnoreSoftDelete();
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("refresh_tokens");
        b.HasKey(t => t.Id);
        b.Ignore(t => t.User);
        b.Ignore(t => t.CreatedByIp);    // inet column; IP logging not persisted from app
    }
}

public sealed class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> b)
    {
        b.ToTable("alerts");
        b.HasKey(a => a.Id);
        b.Property(a => a.Priority).HasColumnName("severity");
    }
}

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> b)
    {
        b.ToTable("documents");
        b.HasKey(d => d.Id);
        b.HasQueryFilter(d => !d.IsDeleted);
    }
}

public sealed class CompanySettingConfiguration : IEntityTypeConfiguration<CompanySetting>
{
    public void Configure(EntityTypeBuilder<CompanySetting> b)
    {
        b.ToTable("company_settings");
        b.HasKey(c => c.Id);
        b.Ignore("CreatedBy");           // company_settings has no created_by / soft-delete
        b.IgnoreSoftDelete();
    }
}

public sealed class ReportFileConfiguration : IEntityTypeConfiguration<ReportFile>
{
    public void Configure(EntityTypeBuilder<ReportFile> b)
    {
        b.ToTable("report_files");
        b.HasKey(r => r.Id);
        b.Property(r => r.Parameters).HasColumnType("jsonb");
    }
}

public sealed class VehicleTrailerAssignmentConfiguration : IEntityTypeConfiguration<VehicleTrailerAssignment>
{
    public void Configure(EntityTypeBuilder<VehicleTrailerAssignment> b)
    {
        b.ToTable("vehicle_trailer_assignments");
        b.HasKey(a => a.Id);
        b.Ignore(a => a.IsActive);
        b.Ignore(a => a.Vehicle);
        b.Ignore(a => a.Trailer);
        b.IgnoreSoftDelete();
    }
}

public sealed class DriverVehicleAssignmentConfiguration : IEntityTypeConfiguration<DriverVehicleAssignment>
{
    public void Configure(EntityTypeBuilder<DriverVehicleAssignment> b)
    {
        b.ToTable("driver_vehicle_assignments");
        b.HasKey(a => a.Id);
        b.Ignore(a => a.IsActive);
        b.Ignore(a => a.Driver);
        b.Ignore(a => a.Vehicle);
        b.IgnoreSoftDelete();
    }
}
