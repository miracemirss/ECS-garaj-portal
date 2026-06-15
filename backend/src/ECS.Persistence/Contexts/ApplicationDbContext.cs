using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace ECS.Persistence.Contexts;

/// <summary>
/// EF Core database context. DbSet&lt;T&gt; properties and entity configurations
/// are added per entity in later prompts; OnModelCreating already wires up every
/// IEntityTypeConfiguration in this assembly.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Example (added later): public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
