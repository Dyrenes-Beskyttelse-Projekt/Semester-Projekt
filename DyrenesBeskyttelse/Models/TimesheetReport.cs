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
}