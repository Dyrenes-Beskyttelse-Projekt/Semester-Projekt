namespace DyrenesBeskyttelse.Components.Models;

// US-9.1 (Mikes task, lavet af Julie d. 17/11 fra branchen Julie-sprint2):
// Aktivitetsreglen som en klasse uden database. Den får datoerne for en frivilligs fremmøde og
// svarer på, om den frivillige overholder reglen i en given måned. Den ved intet om databasen,
// og derfor kan den unit testes uden SQLite (US-9.2, se ActivityRuleTests).
//
// Reglen (acceptkriteriet i US-9, tabel 3.1 i rapporten):
//   - Gamle frivillige (startet før 2026) markeres, hvis der er MERE end 27 dage mellem to fremmøder.
//   - Nye frivillige (startet i 2026 eller senere) markeres, hvis der er MERE end 13 dage mellem to fremmøder.
//   - Tiden fra sidste fremmøde før måneden (eller månedens start) til første fremmøde og fra sidste
//     fremmøde til månedens slutning tæller også med.
//   - En frivillig uden fremmøde i måneden markeres altid.
//
// Sådan bruges den i månedsrapporten (Simons US-8), fx for hver Timesheet:
//   bool brud = ActivityRule.BreaksRule(volunteer.StartedBefore2026,
//                   sessions.Select(s => s.SessionStart), year, month);
public static class ActivityRule
{
    // Det største tilladte mellemrum i dage. Ét dag mere, så er reglen brudt.
    public const int MaxGapOld = 27;  // startet før 2026: mindst hver 14. dag
    public const int MaxGapNew = 13;  // startet i 2026 eller senere: mindst én gang om ugen

    public static int MaxAllowedGap(bool startedBefore2026)
    {
        return startedBefore2026 ? MaxGapOld : MaxGapNew;
    }

    // Det største mellemrum i dage i den valgte måned.
    // dates: tidspunkter for tjek ind (kun datoen bruges, og flere gange samme dag tæller som én).
    // Datoer uden for måneden ignoreres.
    // lastBeforeMonth: sidste fremmøde før måneden, hvis man kender det. Ellers regnes der fra
    // månedens start (d. 1.).
    // Returnerer null, hvis der intet fremmøde er i måneden (så er reglen altid brudt).
    public static int? LargestGap(IEnumerable<DateTime> dates, int year, int month, DateTime? lastBeforeMonth = null)
    {
        var monthStart = new DateTime(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        // Kun datoer i måneden, hver dato én gang, i rækkefølge.
        var days = dates
            .Select(d => d.Date)
            .Where(d => d >= monthStart && d <= monthEnd)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        if (days.Count == 0)
        {
            return null;
        }

        // Starten: sidste fremmøde før måneden, hvis det kendes, ellers månedens start.
        var previous = lastBeforeMonth?.Date < monthStart ? lastBeforeMonth.Value.Date : monthStart;

        var largest = 0;
        foreach (var day in days)
        {
            largest = Math.Max(largest, (day - previous).Days);
            previous = day;
        }

        // Fra sidste fremmøde til månedens slutning.
        largest = Math.Max(largest, (monthEnd - previous).Days);

        return largest;
    }

    // true = den frivillige overholder IKKE reglen og skal markeres i rapporten (US-9).
    public static bool BreaksRule(bool startedBefore2026, IEnumerable<DateTime> dates, int year, int month,
        DateTime? lastBeforeMonth = null)
    {
        var gap = LargestGap(dates, year, month, lastBeforeMonth);

        // Intet fremmøde i måneden -> altid markeret (acceptkriteriet i US-9).
        if (gap == null)
        {
            return true;
        }

        return gap > MaxAllowedGap(startedBefore2026);
    }
}
