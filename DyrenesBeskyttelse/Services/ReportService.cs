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
            var sessions = volunteer.Sessions
                .Where(s => s.SessionStart >= start && s.SessionStart <= end)
                .ToList();

            return Timesheet.Create(volunteer.GetFullName(), sessions);
        }).ToList();
    }

    public async Task<TimesheetReport> GetReport(DateTime start, DateTime end)
    {
        List<Timesheet> timesheets = this._GetTimesheetsAsync(start, end).Result;
        
        return new TimesheetReport(start, end, timesheets);
    }

}