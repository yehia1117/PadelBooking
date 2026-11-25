using System.ComponentModel.DataAnnotations;
namespace PadelBooking.Models;
public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "اكتب كلمة المرور الحالية.")]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = "اكتب كلمة المرور الجديدة.")]
    [MinLength(6, ErrorMessage = "كلمة المرور لازم تكون 6 أحرف على الأقل.")]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "أكد كلمة المرور الجديدة.")]
    [Compare(nameof(NewPassword), ErrorMessage = "كلمتا المرور غير متطابقتين.")]
    public string ConfirmPassword { get; set; } = "";
}
