using DyrenesBeskyttelse.Components.Models;
using DyrenesBeskyttelse.Components.Services;
using Microsoft.EntityFrameworkCore;

namespace DyrenesBeskyttelse.Data;

// Julie (sprint 2): Testfrivillige, så listen på tabletten og i administrationen er længere,
// når programmet åbnes. Det bruges til afprøvning og til besøget hos Dyrenes Beskyttelse.
//
// Hvorfor i koden og ikke bare i databasen? Databasefilen (dyrenesbeskyttelse.db) er ikke med i Git,
// så hver af os har sin egen database. Når testdata står her, får alle i gruppen de samme frivillige.
//
// Kaldes fra Program.cs ved opstart, efter MigrateAsync. En frivillig oprettes kun, hvis der ikke
// allerede findes én med samme for- og efternavn. Derfor bliver der ikke lavet dubletter, når
// programmet startes igen, og frivillige, man selv har oprettet, bliver ikke rørt.
public static class TestData
{
    // Julie (US-4.5, acceptkriterie i US-4: "Lederen og frivilligkoordinatoren har hver sin bruger, som vi opretter"):
    // Testbrugere til lederen og frivilligkoordinatoren. Det er TESTkodeord til udvikling og afprøvning.
    // De rigtige brugere med rigtige navne, e-mails og kodeord oprettes ved overdragelsen til
    // Dyrenes Beskyttelse. Rigtige kodeord må aldrig stå her, fordi koden ligger på GitHub.
    private static readonly (string FirstName, string LastName, string Email, string Password)[] Admins =
    {
        ("Leder", "Testbruger", "leder@dyrenesbeskyttelse.dk", "Leder123!"),
        ("Frivilligkoordinator", "Testbruger", "frivilligkoordinator@dyrenesbeskyttelse.dk", "Koordinator123!"),
    };

    // Oprettes kun, hvis der ikke allerede findes en administrator med den e-mail.
    // CreateAdminAsync hasher kodeordet med BCrypt, ligesom når testadministratoren oprettes.
    public static async Task SeedAdminsAsync(AppDbContext db, AdminService adminService)
    {
        // Den gamle testadministrator "Admin" er erstattet af lederen og frivilligkoordinatoren.
        // Findes den stadig i ens database (fra før 17/11), slettes den her.
        var oldAdmin = await db.Admins.FirstOrDefaultAsync(a => a.Email == "admin@dyrenesbeskyttelse.dk");
        if (oldAdmin != null)
        {
            db.Admins.Remove(oldAdmin);
            await db.SaveChangesAsync();
        }

        foreach (var (firstName, lastName, email, password) in Admins)
        {
            if (!await db.Admins.AnyAsync(a => a.Email == email))
            {
                await adminService.CreateAdminAsync(firstName, lastName, "Dyrenes Beskyttelse, Tranbjerg",
                    email, "12345678", password);
            }
        }
    }

    // (fornavn, efternavn, startet før 2026). Blandingen af true/false gør, at begge
    // aktivitetsregler kan afprøves i månedsrapporten (US-8 og US-9).
    private static readonly (string FirstName, string LastName, bool StartedBefore2026)[] Volunteers =
    {
        // Gruppen
        ("Albin", "Meha", false),
        ("Simon", "Stensgaard", false),
        ("Julie", "Bjørn", false),
        ("Ionas", "Belmaïzi", false),
        ("Mike", "Varming", false),
        ("Saed", "Adare", false),

        // Andre testfrivillige
        ("Stine", "Skov-Jensen", true),
        ("Kent", "Hansen", true),
        ("Benn", "Christensen", false),
        ("Klaus", "Bøgestrand", true),
        ("Torben", "Krøjmand", false),
        ("Hanne", "Sommer", true),
        ("Stine", "Hansen", false),
        ("Margrethe", "Dybdahl", true),
        ("Kaj", "Didriksen", true),
    };

    public static async Task SeedVolunteersAsync(AppDbContext db)
    {
        // Navnene, der allerede findes, hentes én gang (i stedet for én forespørgsel pr. frivillig).
        var existing = await db.Volunteers
            .Select(v => v.FirstName + " " + v.LastName)
            .ToListAsync();

        var number = 1;
        foreach (var (firstName, lastName, startedBefore2026) in Volunteers)
        {
            if (!existing.Contains(firstName + " " + lastName))
            {
                db.Volunteers.Add(new Volunteer(
                    firstName,
                    lastName,
                    $"Testvej {number}, 8310 Tranbjerg",
                    MakeEmail(firstName, lastName),
                    $"2010{number:D4}",
                    startedBefore2026));
            }
            number++;
        }

        // Alle nye frivillige gemmes på én gang (én INSERT-omgang).
        await db.SaveChangesAsync();
    }

    // Testregistreringer af fremmøde i oktober og november 2026, så månedsrapporten (US-8) og
    // markeringen af dem, der ikke overholder aktivitetsreglen (US-9), har noget at vise.
    // Gruppens egne navne får ingen registreringer. Dem kan vi tjekke ind live, og de viser
    // samtidig, at en frivillig med 0 dage også kommer med i rapporten (US-8.3).
    //
    // (navn, datoer). En dato, der står to gange, betyder to tjek ind samme dag (US-8.4: datoen
    // skal kun vises én gang). Mønstrene er valgt, så der er nogle, der overholder reglen, og
    // nogle, der ikke gør, og et fremmøde lige ved månedsskiftet (Hanne).
    // OBS: "overholder"/"overholder ikke" gælder OKTOBER, som er en hel måned. I november er der kun
    // registreringer til og med d. 16., og reglen tæller tiden frem til månedens slutning med. Brug
    // derfor oktober, når aktivitetsreglen (US-9) skal afprøves.
    private static readonly (string FullName, DateTime[] Dates)[] Attendance =
    {
        // Mindst én gang om ugen (ny frivillig)
        ("Benn Christensen", Days((10, new[] { 2, 9, 16, 23, 30 }), (11, new[] { 6, 13 }))),      // overholder
        ("Torben Krøjmand", Days((10, new[] { 3, 24 }), (11, new[] { 2 }))),                     // overholder ikke
        ("Stine Hansen", Days((10, new[] { 5, 5, 12, 19, 26 }), (11, new[] { 2, 9, 9, 16 }))),   // overholder, 2 x samme dag

        // Mindst hver 14. dag (startet før 2026)
        ("Stine Skov-Jensen", Days((10, new[] { 7, 20 }), (11, new[] { 3, 14 }))),               // overholder
        ("Kent Hansen", Days((10, new[] { 1 }), (11, new[] { 12 }))),                            // overholder ikke
        ("Klaus Bøgestrand", Days((10, new[] { 10, 22 }), (11, new[] { 4, 15 }))),               // overholder
        ("Hanne Sommer", Days((10, new[] { 31 }), (11, new[] { 1, 13 }))),                       // månedsskiftet
        ("Margrethe Dybdahl", Days((10, new[] { 6, 13, 20, 27 }), (11, new[] { 3, 10 }))),       // overholder
        ("Kaj Didriksen", Days((10, new[] { 8, 21 }))),                                          // intet i november
    };

    // Hjælpemetode: (måned, dage) -> datoer i 2026.
    private static DateTime[] Days(params (int Month, int[] Days)[] months)
    {
        return months
            .SelectMany(m => m.Days.Select(d => new DateTime(2026, m.Month, d)))
            .ToArray();
    }

    public static async Task SeedAttendanceAsync(AppDbContext db)
    {
        var volunteers = await db.Volunteers.Include(v => v.Sessions).ToListAsync();

        foreach (var (fullName, dates) in Attendance)
        {
            // Kun hvis den frivillige findes og endnu ikke har nogen registreringer. Så bliver der ikke
            // lavet dubletter, når programmet startes igen, og rigtige registreringer bliver ikke rørt.
            var volunteer = volunteers.FirstOrDefault(v => v.GetFullName() == fullName);
            if (volunteer == null || volunteer.Sessions.Count > 0)
            {
                continue;
            }

            DateTime? previous = null;
            foreach (var date in dates)
            {
                // Første fremmøde på dagen kl. 9-12. Står datoen to gange, er det andet kl. 13-15.
                var sameDayAgain = previous == date;
                var start = date.AddHours(sameDayAgain ? 13 : 9);
                var end = date.AddHours(sameDayAgain ? 15 : 12);

                // SessionEnd sættes direkte, fordi RegisterEndTime() bruger klokken lige nu.
                // Duration bliver derfor 0, men rapporten bruger kun datoerne.
                volunteer.Sessions.Add(new AttendanceSession(0, start)
                {
                    SessionEnd = end,
                    ExpiredAt = start.ToUniversalTime().AddDays(1)
                });
                previous = date;
            }
        }

        await db.SaveChangesAsync();
    }

    // Fx "Klaus Bøgestrand" -> "klaus.boegestrand@test.dk". Æ, ø, å og ï skrives om,
    // og mellemrum og bindestreger fjernes, så e-mailen er gyldig.
    private static string MakeEmail(string firstName, string lastName)
    {
        var name = (firstName + "." + lastName).ToLower()
            .Replace("æ", "ae").Replace("ø", "oe").Replace("å", "aa").Replace("ï", "i")
            .Replace(" ", "").Replace("-", "");
        return name + "@test.dk";
    }
}
