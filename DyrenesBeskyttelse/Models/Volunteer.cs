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

    public List<AttendanceSession> Sessions { get; set; }

    public Volunteer(string firstName, string lastName, string address, string email, string phoneNumber)
    {
        this.FirstName = firstName;
        this.LastName = lastName;
        this.Address = address;
        this.Email = email;
        this.PhoneNumber = phoneNumber;
        this.IsActive = true;
        this.Sessions = new List<AttendanceSession>();
    }
    
    public string GetFullName()
    {
        return FirstName + " " + LastName;
    }
    
    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
    
    // =================================== /TILFØJET AF MIG =================================== *@
    // public void CreateAttendance(long id, DateTime AttendanceStart);
    
    public void CheckIn()
    {
        Sessions.Add(new AttendanceSession(0, DateTime.Now) );
    }
}