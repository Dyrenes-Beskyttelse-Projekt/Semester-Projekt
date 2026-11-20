using System.ComponentModel.DataAnnotations;

namespace DyrenesBeskyttelse.Components.Models;

// Én registrering af fremmøde (tjek ind og tjek ud).
// 18/11: Hed før "AttendanceSession", men blev omdøbt til "Attendance", så den ikke forveksles med
// AdminSession (hvem der er logget ind). Tabellen hedder nu "Attendances" (migrationen RenameAttendanceSessionToAttendance).
public class Attendance
{
    [Key]
    public long Id { get; set; }

    [Required] 
    public DateTime SessionStart { get; set; } = DateTime.Now;
    
    public DateTime? SessionEnd { get; set; }
    
    public DateTime ExpiredAt { get; set; } = DateTime.UtcNow.AddDays(1);
    
    public long Duration { get; private set; } = 0;

    public Attendance(long id, DateTime sessionStart)
    {
        this.Id = id;   
        this.SessionStart = sessionStart;
    }

    public void RegisterEndTime()
    {
        if (SessionEnd.HasValue)
            return;

        SessionEnd = DateTime.Now;

        Duration = (long)(SessionEnd.Value - SessionStart).TotalMinutes;
    }
    
    public bool IsActive => !SessionEnd.HasValue;
    
    public TimeSpan? GetDuration()
    {
        if (!SessionEnd.HasValue)
            return null;

        return SessionEnd.Value - SessionStart;
    }
}