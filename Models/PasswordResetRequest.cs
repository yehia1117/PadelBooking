namespace PadelBooking.Models;

public class PasswordResetRequest
{
    public int Id { get; set; }
    public string Phone { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsResolved { get; set; }
}
