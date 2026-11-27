using Microsoft.EntityFrameworkCore;
using PadelBooking.Models;

namespace PadelBooking.Data;

public static class DatabaseUpgrade
{
    public static void Apply(AppDbContext db)
    {
        db.Database.EnsureCreated();
        db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS \"CourtSettings\" (\"Id\" INTEGER NOT NULL CONSTRAINT \"PK_CourtSettings\" PRIMARY KEY, \"OpenHour\" INTEGER NOT NULL, \"CloseHour\" INTEGER NOT NULL, \"SlotMinutes\" INTEGER NOT NULL, \"PricePerHour\" TEXT NOT NULL, \"WorkingDays\" TEXT NOT NULL, \"ContactPhone\" TEXT NOT NULL);");
        db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS \"BlockedSlots\" (\"Id\" INTEGER NOT NULL CONSTRAINT \"PK_BlockedSlots\" PRIMARY KEY AUTOINCREMENT, \"StartDateTime\" TEXT NOT NULL, \"EndDateTime\" TEXT NOT NULL, \"Reason\" TEXT NOT NULL);");
        db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS \"PasswordResetRequests\" (\"Id\" INTEGER NOT NULL CONSTRAINT \"PK_PasswordResetRequests\" PRIMARY KEY AUTOINCREMENT, \"Phone\" TEXT NOT NULL, \"CreatedAt\" TEXT NOT NULL, \"IsResolved\" INTEGER NOT NULL);");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS \"IX_Bookings_StartDateTime\" ON \"Bookings\" (\"StartDateTime\");");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS \"IX_Bookings_StartDateTime\" ON \"Bookings\" (\"StartDateTime\");");
        AddColumn(db, "BookingNumber", "TEXT NOT NULL DEFAULT ''");
        AddColumn(db, "ExpiresAt", "TEXT NULL");
        AddColumn(db, "PaymentStatus", "TEXT NOT NULL DEFAULT 'Unpaid'");
        AddColumn(db, "PaidAmount", "TEXT NOT NULL DEFAULT '0'");
        AddColumn(db, "AdminActionBy", "TEXT NULL");
        AddColumn(db, "AdminActionAt", "TEXT NULL");
        AddColumn(db, "AdminActionReason", "TEXT NULL");
        var old = db.Bookings.Where(b => b.BookingNumber == "").OrderBy(b => b.Id).ToList();
        foreach (var b in old) b.BookingNumber = $"PP-{b.CreatedAt.Year}-{b.Id:00000}";
        db.SaveChanges();
        db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Bookings_BookingNumber\" ON \"Bookings\" (\"BookingNumber\");");
        if (!db.CourtSettings.Any()) { db.CourtSettings.Add(new CourtSettings()); db.SaveChanges(); }
    }

   private static void AddColumn(AppDbContext db, string name, string definition)
{
    var allowedColumns = new Dictionary<string, string>
    {
        ["BookingNumber"] = "TEXT NOT NULL DEFAULT ''",
        ["ExpiresAt"] = "TEXT NULL",
        ["PaymentStatus"] = "TEXT NOT NULL DEFAULT 'Unpaid'",
        ["PaidAmount"] = "TEXT NOT NULL DEFAULT '0'",
        ["AdminActionBy"] = "TEXT NULL",
        ["AdminActionAt"] = "TEXT NULL",
        ["AdminActionReason"] = "TEXT NULL"
    };

    if (!allowedColumns.TryGetValue(name, out var safeDefinition))
        throw new InvalidOperationException($"Unsupported column: {name}");

    var connection = db.Database.GetDbConnection();
    var openedHere = connection.State != System.Data.ConnectionState.Open;

    try
    {
        if (openedHere)
            connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Bookings') WHERE name = @name";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@name";
        parameter.Value = name;
        command.Parameters.Add(parameter);

        var exists = Convert.ToInt32(command.ExecuteScalar()) > 0;

        if (!exists)
        {
			db.Database.ExecuteSql(
    $"ALTER TABLE \"Bookings\" ADD COLUMN \"{name}\" {safeDefinition}");
            
        }
    }
    finally
    {
        if (openedHere)
            connection.Close();
    }
}
}
