using System.ComponentModel.DataAnnotations;

namespace DyrenesBeskyttelse.Components.Models;

public class Admin
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
    
    [Required]
    public string PasswordHash { get; set; }

    public Admin(string FirstName, string LastName, string Address, string Email, string PhoneNumber, string PasswordHash)
    {
        this.FirstName = FirstName;
        this.LastName = LastName;
        this.Address = Address;
        this.Email = Email;
        this.PhoneNumber = PhoneNumber;
        this.PasswordHash = PasswordHash;
    }
    
    public string getfullName()
    {
        return FirstName + " " + LastName;
    }
}