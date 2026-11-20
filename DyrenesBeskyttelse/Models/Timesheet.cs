using System.ComponentModel.DataAnnotations;
using DyrenesBeskyttelse.Components.Models;

namespace DyrenesBeskyttelse.Models;

public class Timesheet
{
    public string Name { get; set; }
    
    [Required]
    public List<Attendance> Sessions { get; set; }

    // 18/11 (US-9): Sættes af ReportService ud fra ActivityRule.
    public bool StartedBefore2026 { get; set; }
    public bool BreaksActivityRule { get; set; }

    public Timesheet(string name, List<Attendance> sessions)
    {
        this.Name = name;
        this.Sessions = sessions;
    }

    public static Timesheet Create(string name, List<Attendance> sessions)
    {
        return new Timesheet(name, sessions);
    }
}