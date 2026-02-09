using Microsoft.EntityFrameworkCore;
using WarmaneTracker.Web.Models;

namespace WarmaneTracker.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Item> Items => Set<Item>();
    public DbSet<PriceHistory> PriceHistory => Set<PriceHistory>();
    public DbSet<Stock> Stock => Set<Stock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Item>().HasIndex(x => x.Url).IsUnique();

        modelBuilder.Entity<PriceHistory>()
            .HasOne(x => x.Item)
            .WithMany(x => x.History)
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Stock>()
            .HasOne(x => x.Item)
            .WithOne(x => x.Stock)
            .HasForeignKey<Stock>(x => x.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
