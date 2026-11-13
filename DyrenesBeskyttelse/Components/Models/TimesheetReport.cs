using System.ComponentModel.DataAnnotations;

namespace DyrenesBeskyttelse.Components.Models;

public class TimesheetReport
{
    [Required]
    public long Id  { get; set; }
    
    [Required]
    public DateTime StartDate { get; set; }
    
    [Required]
    public DateTime EndDate { get; set; }
    
    public List<Timesheet> Timesheets { get; set; }

    public TimesheetReport(long id, DateTime startDate, DateTime endDate, List<Timesheet> timesheets)
    {
        Id = id;
        StartDate = startDate;
        EndDate = endDate;
        Timesheets = timesheets;
    }
}