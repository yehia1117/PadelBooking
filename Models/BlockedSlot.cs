namespace PadelBooking.Models;
public class BlockedSlot
{
    public int Id { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string Reason { get; set; } = "غير متاح";
}
