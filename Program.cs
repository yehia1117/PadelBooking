using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PadelBooking.Data;
using PadelBooking.Models;

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
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    // Create new feature tables for installations that already had the original database.
    db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS \"CourtSettings\" (\"Id\" INTEGER NOT NULL CONSTRAINT \"PK_CourtSettings\" PRIMARY KEY, \"OpenHour\" INTEGER NOT NULL, \"CloseHour\" INTEGER NOT NULL, \"SlotMinutes\" INTEGER NOT NULL, \"PricePerHour\" TEXT NOT NULL, \"WorkingDays\" TEXT NOT NULL, \"ContactPhone\" TEXT NOT NULL);");
    db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS \"BlockedSlots\" (\"Id\" INTEGER NOT NULL CONSTRAINT \"PK_BlockedSlots\" PRIMARY KEY AUTOINCREMENT, \"StartDateTime\" TEXT NOT NULL, \"EndDateTime\" TEXT NOT NULL, \"Reason\" TEXT NOT NULL);");
    if (!db.CourtSettings.Any()) { db.CourtSettings.Add(new CourtSettings()); db.SaveChanges(); }
    // Keep the existing administrator account and move its login phone to the court contact number.
    var courtPhone = db.CourtSettings.First().ContactPhone;
    var adminUser = db.Users.FirstOrDefault(u => u.Role == "Admin");
    if (adminUser != null)
    {
        var phoneOwner = db.Users.FirstOrDefault(u => u.Phone == courtPhone && u.Id != adminUser.Id);
        if (phoneOwner != null)
            throw new InvalidOperationException($"رقم الملعب {courtPhone} مستخدم بالفعل لحساب آخر. غيّر رقم هذا الحساب قبل تشغيل الموقع.");
        adminUser.Phone = courtPhone;
        adminUser.Name = "Padel PIN Admin";
        db.SaveChanges();
    }
    else
    {
        var existingCourtPhoneUser = db.Users.FirstOrDefault(u => u.Phone == courtPhone);
        if (existingCourtPhoneUser != null)
        {
            existingCourtPhoneUser.Role = "Admin";
            existingCourtPhoneUser.Name = "Padel PIN Admin";
        }
        else
        {
            db.Users.Add(new User { Name = "Padel PIN Admin", Phone = courtPhone, PasswordHash = PasswordHasher.Hash("Admin@123"), Role = "Admin" });
        }
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
