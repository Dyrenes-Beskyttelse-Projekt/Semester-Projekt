using System.ComponentModel.DataAnnotations;
using DyrenesBeskyttelse.Models;

namespace DyrenesBeskyttelse.Components.Models;

public class TimesheetReport
{
    [Required]
    public DateTime StartDate { get; set; }
    
    [Required]
    public DateTime EndDate { get; set; }
    
    public List<Timesheet> Timesheets { get; set; }

    public TimesheetReport(DateTime startDate, DateTime endDate, List<Timesheet> timesheets)
    {
        StartDate = startDate;
        EndDate = endDate;
        Timesheets = timesheets;
    }

    public static TimesheetReport Create(DateTime startDate, DateTime endDate, List<Timesheet> timesheets)
    {
        return new TimesheetReport(startDate, endDate, timesheets);
    }
    
    // US-8.8: Dem der bryder reglen først, derefter færrest fremmødedage, derefter alfabetisk.
    public List<Timesheet> GetSortedTimesheets()
    {
        return Timesheets
            .OrderByDescending(t => t.BreaksActivityRule)
            .ThenBy(t => t.Sessions.Select(s => s.SessionStart.Date).Distinct().Count())
            .ThenBy(t => t.Name)
            .ToList();
    }

// Til linjen "X af Y overholder ikke aktivitetsreglen".
    public int CountBreakingRule()
    {
        return Timesheets.Count(t => t.BreaksActivityRule);
    }
}