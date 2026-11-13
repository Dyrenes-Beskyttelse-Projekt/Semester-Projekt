using DyrenesBeskyttelse.Components.Models;

namespace DyrenesBeskyttelse.Components.Services;

public class AdminService
{
    public List<Volunteer> Volunteers { get; set; }
    public List<Admin> Admins { get; set; }

    public void CreateVolunteer(string firstName, string lastName, string address, string email, string phoneNumber)
    {
        Volunteers.Add(new Volunteer(firstName, lastName, address, email, phoneNumber));
    }
}