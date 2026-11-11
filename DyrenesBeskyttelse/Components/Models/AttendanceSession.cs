using System.ComponentModel.DataAnnotations;

namespace DyrenesBeskyttelse.Components.Models;

public class AttendanceSession
{
    [Key]
    public long Id { get; set; }

    [Required] 
    public DateTime SessionStart { get; set; } = DateTime.Now;
    
    public DateTime? SessionEnd { get; set; }
    
    public long Duration { get; private set; } = 0;

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