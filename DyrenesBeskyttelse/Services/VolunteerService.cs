using DyrenesBeskyttelse.Components.Models;
using DyrenesBeskyttelse.Data;
using Microsoft.EntityFrameworkCore;

namespace DyrenesBeskyttelse.Components.Services;

public class VolunteerService
{
    private AppDbContext _db;

    // Når DI laver en ny VolunteerService, laver den også en ny AppDbContext og sender den med ind her
    // (registreret med AddDbContext i Program.cs). Vi skriver ikke selv "new AppDbContext()".
    public VolunteerService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Volunteer>> GetVolunteersAsync()
    {
        return await _db.Volunteers.ToListAsync();
    }
}
