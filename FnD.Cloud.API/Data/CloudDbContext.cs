using FnD.Cloud.API.Models;
using Microsoft.EntityFrameworkCore;

namespace FnD.Cloud.API.Data;

public class CloudDbContext : DbContext
{
    public CloudDbContext(DbContextOptions<CloudDbContext> options) : base(options) { }

    // We will add your cloud tables here soon!
    public DbSet<CloudOrder> Orders { get; set; }
    public DbSet<CloudOrderItem> OrderItems { get; set; }
    public DbSet<SyncLog> SyncLogs { get; set; }
    public DbSet<Product> Products { get; set; }
}