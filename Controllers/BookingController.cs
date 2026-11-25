using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PadelBooking.Data;
using PadelBooking.Models;
namespace PadelBooking.Controllers;
[Authorize]
public class BookingController : Controller
{
    private readonly AppDbContext _db;
    public BookingController(AppDbContext db) => _db=db;
    private int UserId=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public async Task<IActionResult> Index(string? date)
    {
        var chosen=DateTime.TryParseExact(date,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var parsed)?parsed.Date:DateTime.Today;
        if(chosen<DateTime.Today) chosen=DateTime.Today;
        var s=await _db.CourtSettings.FirstOrDefaultAsync()??new CourtSettings();
        var slots=new List<DateTime>(); var start=chosen.AddHours(s.OpenHour); var end=chosen.AddHours(s.CloseHour);
        if(s.WorkingDays.Split(',').Contains(((int)chosen.DayOfWeek).ToString()))
            for(var x=start;x.AddMinutes(s.SlotMinutes)<=end;x=x.AddMinutes(s.SlotMinutes)) slots.Add(x);
        var booked=await _db.Bookings.Where(b=>b.StartDateTime<end && b.EndDateTime>start && b.Status!="Rejected" && b.Status!="Cancelled").Select(b=>new {b.StartDateTime,b.EndDateTime}).ToListAsync();
        var blocked=await _db.BlockedSlots.Where(b=>b.StartDateTime<end && b.EndDateTime>start).Select(b=>new {b.StartDateTime,b.EndDateTime}).ToListAsync();
        ViewBag.Date=chosen.ToString("yyyy-MM-dd"); ViewBag.Booked=booked; ViewBag.Blocked=blocked; ViewBag.Settings=s;
        return View(slots);
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DateTime startDateTime)
    {
        var s=await _db.CourtSettings.FirstOrDefaultAsync()??new CourtSettings(); var end=startDateTime.AddMinutes(s.SlotMinutes);
        var sessionDate = startDateTime.Hour < s.OpenHour ? startDateTime.Date.AddDays(-1) : startDateTime.Date;
        var dayStart=sessionDate.AddHours(s.OpenHour); var dayEnd=sessionDate.AddHours(s.CloseHour);
        bool valid=startDateTime>=DateTime.Now && startDateTime>=dayStart && end<=dayEnd && (startDateTime - dayStart).TotalMinutes % s.SlotMinutes == 0 && s.WorkingDays.Split(',').Contains(((int)sessionDate.DayOfWeek).ToString());
        bool conflict=await _db.Bookings.AnyAsync(b=>b.StartDateTime<end && b.EndDateTime>startDateTime && b.Status!="Rejected" && b.Status!="Cancelled") || await _db.BlockedSlots.AnyAsync(b=>b.StartDateTime<end && b.EndDateTime>startDateTime);
        if(!valid||conflict){TempData["Message"]="الموعد غير متاح أو تم حجزه بالفعل.";return RedirectToAction(nameof(Index),new {date=startDateTime.Date.ToString("yyyy-MM-dd")});}
        _db.Bookings.Add(new Booking{UserId=UserId,StartDateTime=startDateTime,EndDateTime=end,Status="Pending"});
        try{await _db.SaveChangesAsync();TempData["Message"]="تم إرسال طلب الحجز، وستظهر الموافقة في صفحة حجوزاتي.";}catch(DbUpdateException){TempData["Message"]="الموعد اتاخد للتو، اختار موعد تاني.";}
        return RedirectToAction(nameof(MyBookings));
    }
    public async Task<IActionResult> MyBookings()=>View(await _db.Bookings.Where(b=>b.UserId==UserId).OrderByDescending(b=>b.StartDateTime).ToListAsync());
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id){var b=await _db.Bookings.FirstOrDefaultAsync(x=>x.Id==id&&x.UserId==UserId);if(b!=null&&b.Status=="Pending"&&b.StartDateTime>DateTime.Now){b.Status="Cancelled";await _db.SaveChangesAsync();TempData["Message"]="تم إلغاء الطلب.";}return RedirectToAction(nameof(MyBookings));}
}
