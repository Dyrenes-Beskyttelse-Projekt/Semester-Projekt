using System.ComponentModel.DataAnnotations;

namespace DyrenesBeskyttelse.Components.Models;

public class TimesheetReport
{
    [Required]
    public long id  { get; set; }
    
    [Required]
    public DateTime startDate { get; set; }
    
    [Required]
    public DateTime endDate { get; set; }
    
    public List<Timesheet> timesheets { get; set; }
}