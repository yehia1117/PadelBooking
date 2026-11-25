using Microsoft.EntityFrameworkCore;
using PadelBooking.Models;
namespace PadelBooking.Data;
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<User> Users => Set<User>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<CourtSettings> CourtSettings => Set<CourtSettings>();
    public DbSet<BlockedSlot> BlockedSlots => Set<BlockedSlot>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(u => u.Phone).IsUnique();
        modelBuilder.Entity<Booking>().HasIndex(b => b.StartDateTime).IsUnique();
        modelBuilder.Entity<Booking>().HasOne(b => b.User).WithMany(u => u.Bookings).HasForeignKey(b => b.UserId);
        modelBuilder.Entity<CourtSettings>().HasKey(x => x.Id);
        modelBuilder.Entity<BlockedSlot>().HasIndex(x => new { x.StartDateTime, x.EndDateTime });
    }
}
