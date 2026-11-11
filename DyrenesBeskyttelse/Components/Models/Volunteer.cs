using System.ComponentModel.DataAnnotations;

namespace DyrenesBeskyttelse.Components.Models;

public class Volunteer
{
    [Key]
    public long Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; }
    
    [Required]
    [StringLength(100)]
    public string LastName { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Address { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Email { get; set; }
    
    [Required]
    [StringLength(100)]
    public string PhoneNumber { get; set; }

    [Required] public bool IsActive { get; set; } = false;

    public List<AttendanceSession> Sessions { get; set; } = new List<AttendanceSession>();

    public Volunteer(string firstName, string lastName, string address, string email, string phoneNumber)
    {
        this.FirstName = firstName;
        this.LastName = lastName;
        this.Address = address;
        this.Email = email;
        this.PhoneNumber = phoneNumber;
        this.IsActive = false;
        this.Sessions = new List<AttendanceSession>();
    }
    
    public string GetfullName()
    {
        return FirstName + " " + LastName;
    }
    
    public void Activate()
    {
        IsActive = true;
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