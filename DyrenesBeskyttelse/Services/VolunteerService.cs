using DyrenesBeskyttelse.Components.Models;
using DyrenesBeskyttelse.Data;
using Microsoft.EntityFrameworkCore;

namespace DyrenesBeskyttelse.Components.Services;

public class VolunteerService
{
    private AppDbContext _db;

    // Når DI laver en ny VolunteerService, laver den også en ny AppDbContext og sender den med ind her
    // (registreret med AddDbContext i Program.cs). Vi skriver ikke selv "new AppDbContext()".
    public VolunteerService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Volunteer>> GetVolunteersAsync()
    {
        return await _db.Volunteers.ToListAsync();
    }

    //  Tabletten (US-1, US-2, US-3) 
    /* OVERLAP:
     Mine tasks er brugerfladen: listen med navne (US-1.1), kun navne (US-1.3) og
     tjek ud-GUI'en (US-3.2). Brugerfladen kan ikke køres eller testes uden noget at hente
     data fra og trykke på. Vi aftalte i gruppen, at ingen skulle vente på hinanden i weekenden,
     så de manglende dele er lavet her ud fra antagelser, selvom de er andres tasks:
       - GetTabletListAsync  -> Simons US-1.2 (hente aktive navne alfabetisk),
                                US-1.4 (inaktive vises ikke) og US-3.4 (er man tjekket ind?)
       - CheckInAsync        -> Jonas' US-2.3 (gem tjek ind)
       - CheckOutAsync       -> Jonas' US-3.3 (gem tjek ud)
     Mandag: Simon + jeg og Jonas + jeg gennemgår koden sammen, vælger én udgave af hver
     metode og ét navn, kører testene (dotnet test) og merger FØRST derefter.*/

    // OVERLAP med Simons US-1.2, US-1.4 og US-3.4. Bruges af mine US-1.1 og US-1.3.
    // Kun aktive frivillige, alfabetisk, og kun id, navn og tidspunkt for tjek ind i dag.
    // Select'en sker i databasen, så kontaktoplysningerne slet ikke bliver hentet (US-1.3).
    public async Task<List<TabletVolunteer>> GetTabletListAsync()
    {
        // DateTime.Today lægges i en variabel, så EF Core sender den som parameter (lokal tid)
        // i stedet for at oversætte den til SQLite's date('now'), som er UTC.
        var today = DateTime.Today;

        var volunteers = await _db.Volunteers
            .Where(v => v.IsActive)
            .Select(v => new TabletVolunteer(
                v.Id,
                v.FirstName + " " + v.LastName,
                v.Sessions
                    .Where(s => s.SessionEnd == null && s.SessionStart >= today)
                    .OrderByDescending(s => s.SessionStart)
                    .Select(s => (DateTime?)s.SessionStart)
                    .FirstOrDefault()))
            .ToListAsync();

        return TabletVolunteer.SortByName(volunteers);
    }

    // OVERLAP med Jonas' US-2.3. Lavet, så knappen "Tjek ind" i dialogen kan afprøves,
    // og så der findes frivillige, der er tjekket ind, når tjek ud-GUI'en (US-3.2) skal testes.
    // US-2: Kun muligt, når man ikke allerede er tjekket ind i dag.
    public async Task<bool> CheckInAsync(long volunteerId)
    {
        var today = DateTime.Today;

        var volunteer = await _db.Volunteers
            .Include(v => v.Sessions.Where(s => s.SessionEnd == null && s.SessionStart >= today))
            .FirstOrDefaultAsync(v => v.Id == volunteerId && v.IsActive);

        if (volunteer == null || volunteer.Sessions.Count > 0)
        {
            return false;
        }

        volunteer.addSession(new AttendanceSession(0, DateTime.Now));
        await _db.SaveChangesAsync();
        return true;
    }

    // OVERLAP med Jonas' US-3.3. Lavet, så knappen "Tjek ud" i min tjek ud-GUI (US-3.2) gør noget.
    // Reglen "kun tjek ud, når man er tjekket ind" står både her og i GUI'en, så den holder, selvom nogen går uden om knappen.
    // US-3: Kun muligt, når man er tjekket ind i dag (US-3.2).
    public async Task<bool> CheckOutAsync(long volunteerId)
    {
        var today = DateTime.Today;

        var session = await _db.Volunteers
            .Where(v => v.Id == volunteerId)
            .SelectMany(v => v.Sessions)
            .Where(s => s.SessionEnd == null && s.SessionStart >= today)
            .OrderByDescending(s => s.SessionStart)
            .FirstOrDefaultAsync();

        if (session == null)
        {
            return false;
        }

        session.RegisterEndTime();
        await _db.SaveChangesAsync();
        return true;
    }
    
    // Initialization. Simons automatiske check ud, hvis man har glemt at tjekke ud. 
    public async Task InitializeService(
        CancellationToken cancellationToken = default)
    {
        await CleanupSessions(cancellationToken);
    }

    // Session cleanup
    private async Task CleanupSessions(
        CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;

        var expiredSessions = await _db.AttendanceSessions
            .Where(ses => ses.SessionEnd == null &&
                          ses.ExpiredAt <= now)
            .ToListAsync(cancellationToken);

        foreach (var session in expiredSessions)
        {
            session.RegisterEndTime();
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
