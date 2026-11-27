using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PadelBooking.Data;
using PadelBooking.Models;
namespace PadelBooking.Controllers;
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    public AdminController(AppDbContext db) => _db = db;
    private async Task<CourtSettings> Settings() => await _db.CourtSettings.FirstOrDefaultAsync() ?? new CourtSettings();

    public async Task<IActionResult> Index(string? date)
    {
        var chosen = DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed.Date : DateTime.Today;
        var settings = await Settings();
        var start = chosen.AddHours(settings.OpenHour);
        var end = chosen.AddHours(settings.CloseHour);
        var now = DateTime.Now;
        var expired = await _db.Bookings.Where(b => b.Status == "Pending" && b.ExpiresAt != null && b.ExpiresAt <= now).ToListAsync();
        foreach (var item in expired) item.Status = "Expired";
        if (expired.Count > 0) await _db.SaveChangesAsync();
        var bookings = await _db.Bookings.Include(b => b.User).Where(b => b.StartDateTime >= start && b.StartDateTime < end).OrderBy(b => b.StartDateTime).ToListAsync();
        var blocks = await _db.BlockedSlots.Where(x => x.StartDateTime < end && x.EndDateTime > start).OrderBy(x => x.StartDateTime).ToListAsync();
        ViewBag.Date = chosen.ToString("yyyy-MM-dd"); ViewBag.PendingCount = await _db.Bookings.CountAsync(b => b.Status == "Pending");
        ViewBag.TodayCount = await _db.Bookings.CountAsync(b => b.StartDateTime >= DateTime.Today.AddHours(settings.OpenHour) && b.StartDateTime < DateTime.Today.AddHours(settings.CloseHour) && b.Status != "Cancelled" && b.Status != "Rejected");
        ViewBag.Blocks = blocks; ViewBag.Settings = settings;
        return View(bookings);
    }

    public async Task<IActionResult> Schedule() { ViewBag.Settings = await Settings(); return View(); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSchedule(int openHour, int closeHour, int slotMinutes, decimal pricePerHour, string contactPhone, string[] workingDays)
    {
        if (openHour < 0 || openHour > 23 || closeHour <= openHour || closeHour > 30 || slotMinutes < 30 || slotMinutes > 240 || 60 * (closeHour-openHour) % slotMinutes != 0 || pricePerHour < 0)
        { TempData["Message"] = "راجع ساعات العمل ومدة الحجز والسعر."; return RedirectToAction(nameof(Schedule)); }
        var s = await Settings(); s.OpenHour=openHour; s.CloseHour=closeHour; s.SlotMinutes=slotMinutes; s.PricePerHour=pricePerHour;
        s.ContactPhone=contactPhone?.Trim() ?? ""; s.WorkingDays=string.Join(",", (workingDays ?? Array.Empty<string>()).Where(x => int.TryParse(x,out var d) && d>=0 && d<=6).Distinct());
        await _db.SaveChangesAsync(); TempData["Message"]="تم حفظ إعدادات مواعيد الملعب."; return RedirectToAction(nameof(Schedule));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BlockDay(DateTime date, string? reason)
    {
        var s=await Settings(); var start=date.Date.AddHours(s.OpenHour); var end=date.Date.AddHours(s.CloseHour);
        if (start < DateTime.Now || await _db.Bookings.AnyAsync(b=>b.StartDateTime<end && b.EndDateTime>start && b.Status!="Rejected" && b.Status!="Cancelled")) { TempData["Message"]="لا يمكن إغلاق يوم بدأ بالفعل أو يحتوي على حجوزات نشطة."; return RedirectToAction(nameof(Index),new {date=date.ToString("yyyy-MM-dd")}); }
        var existing=await _db.BlockedSlots.AnyAsync(x=>x.StartDateTime==start && x.EndDateTime==end);
        if(!existing) _db.BlockedSlots.Add(new BlockedSlot{StartDateTime=start,EndDateTime=end,Reason=string.IsNullOrWhiteSpace(reason)?"إغلاق يوم كامل":reason.Trim()});
        await _db.SaveChangesAsync(); TempData["Message"]="تم إغلاق اليوم."; return RedirectToAction(nameof(Index),new {date=date.ToString("yyyy-MM-dd")});
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BlockSlot(DateTime startDateTime, string? reason)
    {
        var s=await Settings(); var end=startDateTime.AddMinutes(s.SlotMinutes);
        if(startDateTime<DateTime.Now || await _db.Bookings.AnyAsync(b=>b.StartDateTime<end && b.EndDateTime>startDateTime && b.Status!="Rejected" && b.Status!="Cancelled")) { TempData["Message"]="لا يمكن إغلاق موعد ماضٍ أو محجوز."; return RedirectToAction(nameof(Index),new {date=startDateTime.Date.ToString("yyyy-MM-dd")}); }
        if(!await _db.BlockedSlots.AnyAsync(x=>x.StartDateTime==startDateTime && x.EndDateTime==end)) _db.BlockedSlots.Add(new BlockedSlot{StartDateTime=startDateTime,EndDateTime=end,Reason=string.IsNullOrWhiteSpace(reason)?"مغلق بواسطة الإدارة":reason.Trim()});
        await _db.SaveChangesAsync(); TempData["Message"]="تم إغلاق الموعد."; return RedirectToAction(nameof(Index),new {date=startDateTime.Date.ToString("yyyy-MM-dd")});
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Unblock(int id) { var x=await _db.BlockedSlots.FindAsync(id); if(x!=null) { var d=x.StartDateTime.Date; _db.BlockedSlots.Remove(x); await _db.SaveChangesAsync(); return RedirectToAction(nameof(Index),new {date=d.ToString("yyyy-MM-dd")}); } return RedirectToAction(nameof(Index)); }

    public async Task<IActionResult> ManualBooking(string? date)
    {
        var d=DateTime.TryParse(date,out var parsed)?parsed.Date:DateTime.Today; ViewBag.Date=d.ToString("yyyy-MM-dd");
        ViewBag.Users=await _db.Users.Where(u=>u.Role!="Admin").OrderBy(u=>u.Name).ToListAsync(); return View();
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateManual(int userId, DateTime startDateTime)
    {
        var s=await Settings(); var end=startDateTime.AddMinutes(s.SlotMinutes); var sessionDate=startDateTime.Hour<s.OpenHour?startDateTime.Date.AddDays(-1):startDateTime.Date; var dayStart=sessionDate.AddHours(s.OpenHour); var dayEnd=sessionDate.AddHours(s.CloseHour);
        if(!await _db.Users.AnyAsync(u=>u.Id==userId && u.Role!="Admin") || startDateTime<DateTime.Now || startDateTime<dayStart || (startDateTime - dayStart).TotalMinutes % s.SlotMinutes != 0 || end>dayEnd || !s.WorkingDays.Split(',').Contains(((int)sessionDate.DayOfWeek).ToString()) || await _db.Bookings.AnyAsync(b=>b.StartDateTime<end && b.EndDateTime>startDateTime && b.Status!="Rejected" && b.Status!="Cancelled") || await _db.BlockedSlots.AnyAsync(x=>x.StartDateTime<end && x.EndDateTime>startDateTime))
        { TempData["Message"]="العميل أو الموعد غير صالح أو غير متاح."; return RedirectToAction(nameof(ManualBooking),new {date=startDateTime.Date.ToString("yyyy-MM-dd")}); }
        var manual = new Booking{UserId=userId,StartDateTime=startDateTime,EndDateTime=end,Status="Confirmed",BookingNumber="TMP-" + Guid.NewGuid().ToString("N"),PaymentStatus="Paid",PaidAmount=(decimal)(end-startDateTime).TotalHours*s.PricePerHour,AdminNotes="حجز يدوي بواسطة الإدارة",AdminActionBy=User.Identity?.Name,AdminActionAt=DateTime.Now};
        _db.Bookings.Add(manual); await _db.SaveChangesAsync(); manual.BookingNumber=$"PP-{manual.CreatedAt.Year}-{manual.Id:00000}"; await _db.SaveChangesAsync();
        TempData["Message"]="تم تسجيل الحجز اليدوي وتأكيده."; return RedirectToAction(nameof(Index),new {date=startDateTime.Date.ToString("yyyy-MM-dd")});
    }

    public async Task<IActionResult> History(string? q, string? from, string? to, string? status)
    {
        var query=_db.Bookings.Include(b=>b.User).AsQueryable();
        if(!string.IsNullOrWhiteSpace(q)) query=query.Where(b=>b.User!.Phone.Contains(q)||b.User.Name.Contains(q));
        if(DateTime.TryParse(from,out var f)) query=query.Where(b=>b.StartDateTime>=f.Date);
        if(DateTime.TryParse(to,out var t)) query=query.Where(b=>b.StartDateTime<t.Date.AddDays(1));
        if(!string.IsNullOrWhiteSpace(status)) query=query.Where(b=>b.Status==status);
        ViewBag.Q=q; ViewBag.From=from; ViewBag.To=to; ViewBag.Status=status;
        return View(await query.OrderByDescending(b=>b.StartDateTime).Take(500).ToListAsync());
    }
    public async Task<IActionResult> Reports(string? month)
    {
        var m=DateTime.TryParseExact(month,"yyyy-MM",CultureInfo.InvariantCulture,DateTimeStyles.None,out var parsed)?new DateTime(parsed.Year,parsed.Month,1):new DateTime(DateTime.Today.Year,DateTime.Today.Month,1);
        var end=m.AddMonths(1); var items=await _db.Bookings.Where(b=>b.StartDateTime>=m && b.StartDateTime<end).ToListAsync();
        ViewBag.Month=m.ToString("yyyy-MM"); ViewBag.Total=items.Count; ViewBag.Confirmed=items.Count(x=>x.Status=="Confirmed"); ViewBag.Pending=items.Count(x=>x.Status=="Pending"); ViewBag.Cancelled=items.Count(x=>x.Status=="Cancelled"||x.Status=="Rejected"); ViewBag.Revenue=items.Where(x=>x.Status=="Confirmed").Sum(x=>(decimal)(x.EndDateTime-x.StartDateTime).TotalHours)* (await Settings()).PricePerHour;
        return View(items.OrderBy(x=>x.StartDateTime).ToList());
    }


    public async Task<IActionResult> DailyReport(string? date)
    {
        var day = DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed.Date : DateTime.Today;
        var settings = await Settings();
        var start = day.AddHours(settings.OpenHour); var end = day.AddHours(settings.CloseHour);
        var bookings = await _db.Bookings.Include(b => b.User).Where(b => b.StartDateTime >= start && b.StartDateTime < end).OrderBy(b => b.StartDateTime).ToListAsync();
        ViewBag.ReportDate = day.ToString("yyyy-MM-dd");
        ViewBag.PaidTotal = bookings.Where(b => b.PaymentStatus == "Paid").Sum(b => b.PaidAmount);
        return View(bookings);
    }

    public async Task<IActionResult> ResetRequests()
    {
        var requests = await _db.PasswordResetRequests.Where(r => !r.IsResolved).OrderBy(r => r.CreatedAt).ToListAsync();
        return View(requests);
    }

    [HttpGet]
    public async Task<IActionResult> ResetCustomerPassword(int id)
    {
        var request = await _db.PasswordResetRequests.FindAsync(id);
        if (request == null || request.IsResolved) return NotFound();
        var customer = await _db.Users.FirstOrDefaultAsync(u => u.Phone == request.Phone && u.Role != "Admin");
        if (customer == null) return NotFound();
        return View(new AdminResetPasswordViewModel { RequestId = request.Id, UserId = customer.Id, CustomerName = customer.Name, Phone = customer.Phone });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetCustomerPassword(AdminResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var request = await _db.PasswordResetRequests.FindAsync(model.RequestId);
        var customer = await _db.Users.FirstOrDefaultAsync(u => u.Id == model.UserId && u.Role != "Admin");
        if (request == null || request.IsResolved || customer == null || customer.Phone != request.Phone) return NotFound();
        customer.PasswordHash = PasswordHasher.Hash(model.NewPassword);
        request.IsResolved = true;
        await _db.SaveChangesAsync();
        TempData["Message"] = "تم تعيين كلمة مرور جديدة. سلّمها للعميل بعد التحقق من هويته، واطلب منه تغييرها بعد الدخول.";
        return RedirectToAction(nameof(ResetRequests));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status, string? notes)
    {
        if (status is not ("Confirmed" or "Rejected" or "Cancelled")) return BadRequest();
        var booking=await _db.Bookings.FindAsync(id); if(booking==null)return NotFound();
        if (status == "Confirmed" && booking.Status == "Expired")
        {
            var conflict = await _db.Bookings.AnyAsync(b => b.Id != id && b.StartDateTime < booking.EndDateTime && b.EndDateTime > booking.StartDateTime && b.Status != "Rejected" && b.Status != "Cancelled" && b.Status != "Expired");
            if (conflict) { TempData["Message"] = "انتهت المهلة والموعد أصبح محجوزاً؛ لا يمكن تأكيد هذا الطلب."; return RedirectToAction("Index", new { date = booking.StartDateTime.Date.ToString("yyyy-MM-dd") }); }
        }
        booking.Status=status; booking.AdminNotes=notes;
        booking.AdminActionBy=User.Identity?.Name; booking.AdminActionAt=DateTime.Now; booking.AdminActionReason=notes;
        if (status == "Confirmed") { booking.PaymentStatus="Paid"; if (booking.PaidAmount <= 0) { var settings=await Settings(); booking.PaidAmount=(decimal)(booking.EndDateTime-booking.StartDateTime).TotalHours*settings.PricePerHour; } }
        await _db.SaveChangesAsync(); TempData["Message"]="تم تحديث حالة الحجز وتسجيل اسم المسؤول ووقت الإجراء.";
        return RedirectToAction("Index",new {date=booking.StartDateTime.Date.ToString("yyyy-MM-dd")});
    }
}
