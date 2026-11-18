using System.ComponentModel.DataAnnotations;
using DyrenesBeskyttelse.Components.Models;

namespace DyrenesBeskyttelse.Models;

public class Timesheet
{
    public string Name { get; set; }
    
    [Required]
    public List<AttendanceSession> Sessions { get; set; }

    public Timesheet(string name, List<AttendanceSession> sessions)
    {
        this.Name = name;
        this.Sessions = sessions;
    }

    public static Timesheet Create(string name, List<AttendanceSession> sessions)
    {
        return new Timesheet(name, sessions);
    }
}