using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PadelBooking.Data;

namespace PadelBooking.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    public HomeController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        ViewBag.Settings = await _db.CourtSettings.FirstOrDefaultAsync();
        return View();
    }

    public IActionResult Error() => View();
}
