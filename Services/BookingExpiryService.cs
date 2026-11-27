using Microsoft.EntityFrameworkCore;
using PadelBooking.Data;

namespace PadelBooking.Services;

public sealed class BookingExpiryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingExpiryService> _logger;
    public BookingExpiryService(IServiceScopeFactory scopeFactory, ILogger<BookingExpiryService> logger)
    { _scopeFactory = scopeFactory; _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var now = DateTime.Now;
                var expired = await db.Bookings.Where(b => b.Status == "Pending" && b.ExpiresAt != null && b.ExpiresAt <= now).ToListAsync(stoppingToken);
                foreach (var booking in expired) booking.Status = "Expired";
                if (expired.Count > 0) await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex) { _logger.LogError(ex, "Booking expiry processing failed"); }
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
