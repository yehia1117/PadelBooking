# Padel PIN Booking System (ASP.NET Core MVC)

Arabic RTL prototype for booking one padel court. Players register with an Egyptian mobile number and password. Requests remain Pending until an admin confirms or rejects them.

## Requirements
- .NET 8 SDK
- Visual Studio 2022 (ASP.NET and web development workload) or VS Code

## Run locally
1. Open `PadelBooking.csproj` in Visual Studio, or open the folder in a terminal.
2. Run:
   ```bash
   dotnet restore
   dotnet run
   ```
3. Open the localhost URL printed in the terminal.

The SQLite database is created automatically as `padel.db` on first run.

## Default admin login
- Phone: the court contact number configured in Court Settings (default `01095672217`)
- Password: `Admin@123`

Change this password before deploying publicly. The starter seeds this admin account only if it does not already exist in the database.

## Booking hours
The daily booking slots start at 4:00 PM and are one hour long, with the last slot starting at midnight and ending at 1:00 AM. The midnight slot is grouped with the previous evening.

## Push to GitHub
Create an empty repository on GitHub, then run from this project directory:

```bash
git init
git add .
git commit -m "Initial Padel PIN Booking System"
git branch -M main
git remote add origin https://github.com/YOUR-USERNAME/Padel-Booking-System.git
git push -u origin main
```

Replace the remote URL with your own repository address. Do not commit the database file or secrets.

## Notes
This is a prototype for testing. It uses cookie authentication and PBKDF2 password hashing. Before public deployment, add rate limiting, stronger account recovery, HTTPS-only cookie settings, production database hosting, admin password management, and booking notifications. GitHub Pages cannot host an ASP.NET MVC application; use an ASP.NET-capable hosting provider for a public test URL.

## New admin features
- Court schedule configuration: opening/closing hours, slot duration, price per hour, working days and contact number.
- Close a full day or block individual time ranges; reopen blocked periods.
- Create manual confirmed bookings for existing registered customers.
- Booking history filters by customer/phone, date range and status.
- Monthly booking and estimated revenue report.
- Customers see booking status and admin notes in My Bookings; contact/WhatsApp links are shown in the footer.

Admin navigation: sign in as admin, then use the links at the top of `/Admin`.

Important: the contact number is set to 01095672217. WhatsApp uses the international Egypt format +201095672217. Automatic WhatsApp/SMS sending is not configured; current notifications are in-app status updates. For automatic outbound messages, connect a WhatsApp Business API/SMS provider.
