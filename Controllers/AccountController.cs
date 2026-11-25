using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PadelBooking.Data;
using PadelBooking.Models;
namespace PadelBooking.Controllers;
public class AccountController : Controller
{
    private readonly AppDbContext _db;
    public AccountController(AppDbContext db) => _db = db;

    [HttpGet] public IActionResult Register() => View();
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        if (await _db.Users.AnyAsync(u => u.Phone == model.Phone)) { ModelState.AddModelError(nameof(model.Phone), "رقم التليفون مسجل بالفعل."); return View(model); }
        var user = new User { Name = model.Name.Trim(), Phone = model.Phone, PasswordHash = PasswordHasher.Hash(model.Password) };
        _db.Users.Add(user); await _db.SaveChangesAsync();
        await SignIn(user);
        return RedirectToAction("Index", "Booking");
    }

    [HttpGet] public IActionResult Login() => View();
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Phone == model.Phone);
        if (user == null || !PasswordHasher.Verify(model.Password, user.PasswordHash)) { ModelState.AddModelError("", "رقم التليفون أو كلمة المرور غير صحيحة."); return View(model); }
        await SignIn(user);
        return user.Role == "Admin" ? RedirectToAction("Index", "Admin") : RedirectToAction("Index", "Booking");
    }

    private async Task SignIn(User user)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, user.Id.ToString()), new(ClaimTypes.Name, user.Name), new(ClaimTypes.MobilePhone, user.Phone), new(ClaimTypes.Role, user.Role) };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }

    [HttpGet]
    public IActionResult ChangePassword() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idClaim, out var userId)) return Challenge();
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return Challenge();
        if (!PasswordHasher.Verify(model.CurrentPassword, user.PasswordHash))
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), "كلمة المرور الحالية غير صحيحة.");
            return View(model);
        }
        if (PasswordHasher.Verify(model.NewPassword, user.PasswordHash))
        {
            ModelState.AddModelError(nameof(model.NewPassword), "اختار كلمة مرور مختلفة عن الحالية.");
            return View(model);
        }
        user.PasswordHash = PasswordHasher.Hash(model.NewPassword);
        await _db.SaveChangesAsync();
        TempData["Message"] = "تم تغيير كلمة المرور بنجاح.";
        return RedirectToAction("ChangePassword");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout() { await HttpContext.SignOutAsync(); return RedirectToAction("Index", "Home"); }
}
