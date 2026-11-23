using DyrenesBeskyttelse.Components.Models;
using DyrenesBeskyttelse.Data;
using Microsoft.EntityFrameworkCore;

namespace DyrenesBeskyttelse.Components.Services;

public record LoginResult(bool Success, string? ErrorMessage = null, Admin? Admin = null);

public enum PaperAttendanceResult
{
    Registered,
    VolunteerNotFound,
    AlreadyCheckedIn
}

public class AdminService
{
    private AppDbContext _db;

    // Når DI laver en ny AdminService, laver den også en ny AppDbContext og sender den med ind her
    // (registreret med AddDbContext i Program.cs). Vi skriver ikke selv "new AppDbContext()".
    public AdminService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Admin>> GetAdminsAsync()
    {
        return await _db.Admins.ToListAsync();
    }

    // Julie (US-7.0) + Saed: Alle frivillige, både aktive og inaktive, til administrationens forside
    // (/admin) og Saeds side /frivillige. Registreringerne (Sessions) hentes med, så Saeds side kan vise,
    // hvem der er tjekket ind lige nu. Sorteres efter navn med dansk alfabet (Æ, Ø, Å til sidst). Det sker
    // efter ToListAsync, fordi SQLite ikke kender den danske rækkefølge.
    // (18/11: Saed og Julie havde begge lavet en metode med dette navn. De er slået sammen til én.)
    public async Task<List<Volunteer>> GetAllVolunteersAsync()
    {
        var volunteers = await _db.Volunteers.Include(v => v.Sessions).ToListAsync();
        var danish = StringComparer.Create(new System.Globalization.CultureInfo("da-DK"), true);
        return volunteers.OrderBy(v => v.GetFullName(), danish).ToList();
    }

    // US-6.2 (Julie): startedBefore2026 kommer fra afkrydsningsfeltet "Startet før 2026" i formularen.
    // Parameteren er valgfri (standard false), så eksisterende kald og tests stadig virker.
    public async Task CreateVolunteerAsync(string firstName, string lastName, string address, string email,
        string phoneNumber, bool startedBefore2026 = false)
    {
        _db.Volunteers.Add(new Volunteer(firstName, lastName, address, email, phoneNumber, startedBefore2026));

        // Først her bliver den frivillige skrevet til databasen (INSERT).
        await _db.SaveChangesAsync();
    }

    public async Task<Admin> CreateAdminAsync(string firstName, string lastName, string address,
        string email, string phoneNumber, string password)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("E-mail må ikke være tom.", nameof(email));
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Kodeord må ikke være tomt.", nameof(password));

        email = email.Trim().ToLower();

        if (await _db.Admins.AnyAsync(a => a.Email == email))
            throw new InvalidOperationException("Der findes allerede en admin med den e-mail.");

        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        var admin = new Admin(firstName, lastName, address, email, phoneNumber, hash);

        _db.Admins.Add(admin);
        await _db.SaveChangesAsync();
        return admin;
    }

    public async Task<LoginResult> LoginAsync(string email, string password)
    {
        const string fejl = "Forkert e-mail eller kodeord.";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return new LoginResult(false, fejl);

        email = email.Trim().ToLower();
        var admin = await _db.Admins.FirstOrDefaultAsync(a => a.Email == email);

        // Samme fejlbesked i begge tilfælde, så man ikke kan gætte hvilke e-mails der findes.
        if (admin is null || !BCrypt.Net.BCrypt.Verify(password, admin.PasswordHash))
            return new LoginResult(false, fejl);

        return new LoginResult(true, null, admin);
    }

    // US-5.7: hent én frivillig (bruges til at forudfylde redigeringssiden)
    public async Task<Volunteer?> GetVolunteerByIdAsync(long id)
    {
        return await _db.Volunteers.FindAsync(id);
    }

    // US-5.7: opdater en frivillig (18/11: + startedBefore2026, US-6 "kan rettes bagefter")
    public async Task<bool> UpdateVolunteerAsync(long id, string firstName, string lastName,
        string address, string email, string phoneNumber, bool startedBefore2026)
    {
        var volunteer = await _db.Volunteers.FindAsync(id);
        if (volunteer == null)
        {
            return false;
        }

        volunteer.UpdateDetails(firstName, lastName, address, email, phoneNumber, startedBefore2026);
        await _db.SaveChangesAsync();
        return true;
    }

    // Skift mellem aktiv og inaktiv
    public async Task<bool> SetVolunteerActiveAsync(long id, bool active)
    {
        var volunteer = await _db.Volunteers.FindAsync(id);
        if (volunteer == null)
            return false;

        if (active) volunteer.Activate();
        else volunteer.Deactivate();

        await _db.SaveChangesAsync();
        return true;
    }

    // US-10.1: Registrer fremmøde fra papirliste
    public async Task<PaperAttendanceResult> RegisterPaperAttendanceAsync(long volunteerId, DateTime start)
    {
        var volunteer = await _db.Volunteers
            .Include(v => v.Sessions)
            .FirstOrDefaultAsync(v => v.Id == volunteerId);
        
        if (volunteer == null) return PaperAttendanceResult.VolunteerNotFound;

        if (volunteer.Sessions.Any(s => s.SessionEnd == null))
            return PaperAttendanceResult.AlreadyCheckedIn;

        volunteer.Sessions.Add(new Attendance(0, start) { SessionEnd = start });
        await _db.SaveChangesAsync();
        return PaperAttendanceResult.Registered;
    }
}