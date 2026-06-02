using FnD.Cloud.API.Infrastructure;
using FnD.Cloud.API.Models;
using Microsoft.EntityFrameworkCore;

namespace FnD.Cloud.API.Data;

public class CloudDbContext : DbContext
{
    private readonly ITenantProvider _tenantProvider;

    public CloudDbContext(DbContextOptions<CloudDbContext> options, ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public DbSet<CloudOrder> Orders => Set<CloudOrder>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<SyncLog> SyncLogs => Set<SyncLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ◄ THE AUTOMATIC SHIELD ►
        // This forces EVERY query targeting the Orders table to implicitly include 
        // a WHERE TenantId = CurrentTenantId filter behind the scenes.
        modelBuilder.Entity<Order>()
            .HasQueryFilter(o => o.TenantId == _tenantProvider.TenantId);
    }

    // Automatically stamp the TenantId on saves so you don't have to assign it manually
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Order>().Where(e => e.State == EntityState.Added))
        {
            entry.Entity.TenantId = _tenantProvider.TenantId;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}