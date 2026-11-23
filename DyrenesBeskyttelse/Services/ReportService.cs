using DyrenesBeskyttelse.Components.Models;
using DyrenesBeskyttelse.Data;
using DyrenesBeskyttelse.Models;
using Microsoft.EntityFrameworkCore;

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

    public async Task<TimesheetReport> GetReport(DateTime start, DateTime end)
    {
        List<Timesheet> timesheets = await this._GetTimesheetsAsync(start, end);
        
        return new TimesheetReport(start, end, timesheets);
    }

}