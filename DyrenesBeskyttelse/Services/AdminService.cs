using DyrenesBeskyttelse.Components.Models;
using DyrenesBeskyttelse.Data;
using Microsoft.EntityFrameworkCore;

namespace DyrenesBeskyttelse.Components.Services;

public class AdminService
{
    private AppDbContext _db;

    // Når DI laver en ny AdminService, laver den også en ny AppDbContext og sender den med ind her
    // (registreret med AddDbContext i Program.cs). Vi skriver ikke selv "new AppDbContext()".
    public AdminService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Admin>> GetAdminsAsync()
    {
        return await _db.Admins.ToListAsync();
    }

    public async Task CreateVolunteerAsync(string firstName, string lastName, string address, string email, string phoneNumber)
    {
        _db.Volunteers.Add(new Volunteer(firstName, lastName, address, email, phoneNumber));

        // Først her bliver den frivillige skrevet til databasen (INSERT).
        await _db.SaveChangesAsync();
    }
}
