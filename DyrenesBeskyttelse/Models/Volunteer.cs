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

    [Required] public bool IsActive { get; set; } = true;

    // Julie (US-6.1): Er den frivillige startet før 2026? Det afgør aktivitetsreglen (US-9):
    //   true  = gammel frivillig -> skal møde mindst hver 14. dag
    //   false = ny frivillig     -> skal møde mindst én gang om ugen
    // Feltet er en ny kolonne i tabellen Volunteers (migrationen AddStartedBefore2026).
    // Eksisterende frivillige får false, indtil administrationen retter det.
    public bool StartedBefore2026 { get; set; }

    public List<AttendanceSession> Sessions { get; set; } = new List<AttendanceSession>();

    // US-6.1: startedBefore2026 er en valgfri parameter (standard false). Så virker de steder,
    // der allerede kalder constructoren med fem parametre (fx testene), uden at skulle ændres.
    public Volunteer(string firstName, string lastName, string address, string email, string phoneNumber,
        bool startedBefore2026 = false)
    {
        this.FirstName = firstName;
        this.LastName = lastName;
        this.Address = address;
        this.Email = email;
        this.PhoneNumber = phoneNumber;
        this.StartedBefore2026 = startedBefore2026;
        this.IsActive = true;
        this.Sessions = new List<AttendanceSession>();
    }

    // US-6.1/US-7.0: Aktivitetsreglen som tekst, så den kan vises i administrationens liste.
    // Mike laver selve beregningen af, om reglen er overholdt (US-9.1).
    public string GetActivityRuleText()
    {
        return StartedBefore2026 ? "Mindst hver 14. dag" : "Mindst én gang om ugen";
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

    // Jonas (US-2.3): Opretter en ny registrering af fremmøde med tidspunktet for tjek ind.
    // Erstatter addSession(). Om tjek ind er tilladt, tjekker VolunteerService.CheckInAsync.
    public void CheckIn()
    {
        Sessions.Add(new AttendanceSession(0, DateTime.Now));
    }
}