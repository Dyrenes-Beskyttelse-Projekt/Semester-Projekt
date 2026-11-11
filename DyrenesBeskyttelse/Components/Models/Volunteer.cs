using System.ComponentModel.DataAnnotations;

namespace DyrenesBeskyttelse.Components.Models;

public class Volunteer
{
    [Key]
    public long Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string firstName { get; set; }
    
    [Required]
    [StringLength(100)]
    public string lastName { get; set; }
    
    [Required]
    [StringLength(100)]
    public string address { get; set; }
    
    [Required]
    [StringLength(100)]
    public string email { get; set; }
    
    [Required]
    [StringLength(100)]
    public string phoneNumber { get; set; }

    [Required] public bool isActive { get; set; } = false;

    public List<AttendanceSession> sessions { get; set; } = new List<AttendanceSession>();
    
    public string getfullName()
    {
        return firstName + " " + lastName;
    }
    
    public void activate()
    {
        isActive = true;
    }

    public void deactivate()
    {
        isActive = false;
    }
    
    public List<AttendanceSession> getSessions()
    {
        return sessions;
    }

    public void addSession(AttendanceSession session)
    {
        sessions.Add(session);
    }
}