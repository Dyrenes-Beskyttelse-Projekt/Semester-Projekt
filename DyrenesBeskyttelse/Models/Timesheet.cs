using System.ComponentModel.DataAnnotations;

namespace DyrenesBeskyttelse.Components.Models;

public class Timesheet
{
    [Required]
    public long Id { get; set; }
    
    [Required]
    public List<AttendanceSession> Sessions { get; set; }

    public Timesheet(long id, List<AttendanceSession> sessions)
    {
        this.Id = id;
        this.Sessions = sessions;
    }
}