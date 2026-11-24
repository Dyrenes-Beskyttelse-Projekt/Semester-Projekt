using DyrenesBeskyttelse.Components.Models;
using DyrenesBeskyttelse.Data;
using DyrenesBeskyttelse.Models;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using System.Globalization;

namespace DyrenesBeskyttelse.Services;

public class ReportService
{
    private AppDbContext _db;
    
    public ReportService(AppDbContext db)
    {
        _db = db;
    }
    
    public async Task<List<Volunteer>> _GetVolunteersAsync()
    {
        return await _db.Volunteers.ToListAsync();
    }

    
    public async Task<List<Timesheet>> _GetTimesheetsAsync(DateTime start, DateTime end)
    {
        var volunteers = await _db.Volunteers
            .Include(v => v.Sessions)
            .ToListAsync();

        return volunteers.Select(volunteer =>
        {
            // 18/11: Slutdatoen tæller med hele dagen. Før blev fremmøde PÅ slutdatoen ikke talt med,
            // fordi datovælgeren giver slutdatoen kl. 00:00.
            var endExclusive = end.Date.AddDays(1);
            var sessions = volunteer.Sessions
                .Where(s => s.SessionStart >= start && s.SessionStart < endExclusive)
                .ToList();

            var timesheet = Timesheet.Create(volunteer.GetFullName(), sessions);
            timesheet.VolunteerId = volunteer.Id;    
            timesheet.IsActive = volunteer.IsActive; 
            timesheet.RuleText = volunteer.GetActivityRuleText();

            // 18/11 (US-9): Overholder den frivillige aktivitetsreglen i perioden? (se Models/ActivityRule.cs)
            // Sidste fremmøde før perioden tæller med, så mellemrummet ved periodens start bliver rigtigt.
            var lastBefore = volunteer.Sessions
                .Where(s => s.SessionStart < start)
                .Select(s => (DateTime?)s.SessionStart)
                .Max();
            timesheet.StartedBefore2026 = volunteer.StartedBefore2026;
            timesheet.BreaksActivityRule = ActivityRule.BreaksRule(volunteer.StartedBefore2026,
                sessions.Select(s => s.SessionStart), start, end, lastBefore);

            return timesheet;
        }).ToList();
    }
    
    
    // 24/11 (US-13.1): Excel-filen har samme kolonner og rækkefølge som rapporten på skærmen
    // (Overview.razor): Navn, Fremmøde og Aktivitetsregel, kun aktive, brud på reglen øverst med rødt.
    // Liggende format og tilpasset til én sides bredde, så den kan printes.
    public byte[] GenerateExcelReport(TimesheetReport report)
    {
        var dansk = new CultureInfo("da-DK");
        using var workbook = new XLWorkbook();
        var ark = workbook.Worksheets.Add("Månedsrapport");

        ark.Cell("A1").Value = "Månedsrapport";
        ark.Cell("A1").Style.Font.Bold = true;
        ark.Cell("A1").Style.Font.FontSize = 16;
        ark.Cell("A2").Value = "Måned: " + report.StartDate.ToString("MMMM yyyy", dansk);

        const int overskriftRaekke = 4;
        string[] kolonner = { "Navn", "Fremmøde", "Aktivitetsregel" };
        for (int i = 0; i < kolonner.Length; i++)
        {
            ark.Cell(overskriftRaekke, i + 1).Value = kolonner[i];
        }
        var overskrift = ark.Range(overskriftRaekke, 1, overskriftRaekke, kolonner.Length);
        overskrift.Style.Font.Bold = true;
        overskrift.Style.Fill.BackgroundColor = XLColor.LightGray;

        int raekke = overskriftRaekke + 1;
        foreach (var t in report.GetSortedTimesheets())
        {
            ark.Cell(raekke, 1).Value = t.Name;
            ark.Cell(raekke, 2).Value = AttendanceText(t);
            ark.Cell(raekke, 3).Value = (t.BreaksActivityRule ? "overholdes ikke" : "overholdes")
                                        + $" ({t.RuleText})";

            if (t.BreaksActivityRule)
            {
                var linje = ark.Range(raekke, 1, raekke, kolonner.Length);
                linje.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8D7DA");
                linje.Style.Font.FontColor = XLColor.FromHtml("#7E0D00");
            }
            raekke++;
        }

        ark.Columns().AdjustToContents();
        ark.Column(2).Width = Math.Min(ark.Column(2).Width, 60);
        ark.Column(2).Style.Alignment.WrapText = true;
        ark.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        ark.PageSetup.FitToPages(1, 0);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // 24/11 (US-8.1): Måneder til rullemenuen: de seneste afsluttede måneder, nyeste først.
    // Den indeværende måned er ikke afsluttet og kommer ikke med. Virker også ved årsskiftet:
    // D. 1. januar 2027 står december 2026 øverst.
    public static List<DateTime> GetCompletedMonths(DateTime today, int count = 12)
    {
        var thisMonth = new DateTime(today.Year, today.Month, 1);
        return Enumerable.Range(1, count)
            .Select(i => thisMonth.AddMonths(-i))
            .ToList();
    }

    // 24/11 (US-13.1): Fremmøde som i rapporten: datoerne én gang hver ("dd/MM") eller "0 dage".
    public static string AttendanceText(Timesheet t)
    {
        if (t.Sessions.Count == 0)
        {
            return "0 dage";
        }
        return string.Join(", ", t.Sessions
            .Select(s => s.SessionStart.Date)
            .Distinct()
            .OrderBy(d => d)
            .Select(d => d.ToString("dd/MM", CultureInfo.InvariantCulture)));
    }

    public async Task<TimesheetReport> GetReport(DateTime start, DateTime end)
    {
        List<Timesheet> timesheets = await this._GetTimesheetsAsync(start, end);
        
        return new TimesheetReport(start, end, timesheets);
    }
}