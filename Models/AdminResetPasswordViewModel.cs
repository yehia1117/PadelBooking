using System.ComponentModel.DataAnnotations;
namespace PadelBooking.Models;
public class AdminResetPasswordViewModel
{
    public int UserId { get; set; }
    public int RequestId { get; set; }
    public string CustomerName { get; set; } = "";
    public string Phone { get; set; } = "";
    [Required, MinLength(6, ErrorMessage = "كلمة المرور لازم تكون 6 أحرف على الأقل.")]
    public string NewPassword { get; set; } = "";
    [Required, Compare(nameof(NewPassword), ErrorMessage = "كلمتا المرور غير متطابقتين.")]
    public string ConfirmPassword { get; set; } = "";
}
