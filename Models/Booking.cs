namespace PadelBooking.Models;

public class Booking
{
    public int Id { get; set; }
    public string BookingNumber { get; set; } = "";
    public int UserId { get; set; }
    public User? User { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string Status { get; set; } = "Pending";
    public string? AdminNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? ExpiresAt { get; set; }
    public string PaymentStatus { get; set; } = "Unpaid";
    public decimal PaidAmount { get; set; }
    public string? AdminActionBy { get; set; }
    public DateTime? AdminActionAt { get; set; }
    public string? AdminActionReason { get; set; }
}
