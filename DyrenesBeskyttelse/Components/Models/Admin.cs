using System.ComponentModel.DataAnnotations;

namespace DyrenesBeskyttelse.Components.Models;

public class Admin
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
    
    [Required]
    public string passwordHash { get; set; }
    
    public string getfullName()
    {
        return firstName + " " + lastName;
    }
}