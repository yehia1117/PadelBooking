using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PadelBooking.Data;
using PadelBooking.Models;
using PadelBooking.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=padel.db"));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
    options.Cookie.Name = "PadelBookingAuth";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
});
builder.Services.AddAuthorization();
builder.Services.AddHostedService<BookingExpiryService>();
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    DatabaseUpgrade.Apply(db);
    // Preserve existing admin accounts and passwords. Only seed a default admin on a brand-new database.
    if (!db.Users.Any(u => u.Role == "Admin"))
    {
        var courtPhone = db.CourtSettings.First().ContactPhone;
        var existing = db.Users.FirstOrDefault(u => u.Phone == courtPhone);
        if (existing != null) existing.Role = "Admin";
        else db.Users.Add(new User { Name = "Padel PIN Admin", Phone = courtPhone, PasswordHash = PasswordHasher.Hash("Admin@123"), Role = "Admin" });
        db.SaveChanges();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();
