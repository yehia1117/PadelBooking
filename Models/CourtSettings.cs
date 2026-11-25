namespace PadelBooking.Models;
public class CourtSettings
{
    public int Id { get; set; } = 1;
    public int OpenHour { get; set; } = 16;
    public int CloseHour { get; set; } = 25; // 1 AM next day
    public int SlotMinutes { get; set; } = 60;
    public decimal PricePerHour { get; set; } = 0;
    public string WorkingDays { get; set; } = "0,1,2,3,4,5,6"; // Sunday=0
    public string ContactPhone { get; set; } = "01095672217";
}
