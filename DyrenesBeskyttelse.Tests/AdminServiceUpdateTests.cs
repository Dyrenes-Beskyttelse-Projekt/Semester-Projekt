using DyrenesBeskyttelse.Components.Models;
using DyrenesBeskyttelse.Components.Services;
using DyrenesBeskyttelse.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DyrenesBeskyttelse.Tests;

public class AdminServiceUpdateTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly AppDbContext _db;
    private readonly AdminService _service;

    public AdminServiceUpdateTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(_options);
        _db.Database.EnsureCreated();

        _service = new AdminService(_db);
    }

    private async Task<long> OpretFrivillig(string fornavn = "Anna", string efternavn = "Jensen")
    {
        var v = new Volunteer(fornavn, efternavn, "Vej 1", "a@a.dk", "11111111");
        _db.Volunteers.Add(v);
        await _db.SaveChangesAsync();
        return v.Id;
    }

    // Læser med en NY context, så vi ved, at ændringen er gemt i databasen
    private AppDbContext NyContext() => new AppDbContext(_options);

    // US-11.2: ændringer gemmes
    [Fact]
    public async Task UpdateVolunteerAsync_AendrerAlleFelter_OgReturnererTrue()
    {
        var id = await OpretFrivillig();

        var ok = await _service.UpdateVolunteerAsync(id, "Bo", "Hansen", "Vej 2", "b@b.dk", "22222222", true);

        Assert.True(ok);
        using var tjek = NyContext();
        var gemt = await tjek.Volunteers.SingleAsync();
        Assert.Equal("Bo", gemt.FirstName);
        Assert.Equal("Hansen", gemt.LastName);
        Assert.Equal("Vej 2", gemt.Address);
        Assert.Equal("b@b.dk", gemt.Email);
        Assert.Equal("22222222", gemt.PhoneNumber);
        Assert.True(gemt.StartedBefore2026);
    }

    // Ukendt id: intet sker
    [Fact]
    public async Task UpdateVolunteerAsync_UkendtId_ReturnererFalse_OgOpretterIkkeNoget()
    {
        var ok = await _service.UpdateVolunteerAsync(999, "A", "B", "C", "d@d.dk", "1", false);

        Assert.False(ok);
        using var tjek = NyContext();
        Assert.Empty(tjek.Volunteers);
    }

    // Kun den valgte frivillig ændres
    [Fact]
    public async Task UpdateVolunteerAsync_RoererIkkeAndreFrivillige()
    {
        var idAnna = await OpretFrivillig("Anna", "Jensen");
        await OpretFrivillig("Carl", "Madsen");

        await _service.UpdateVolunteerAsync(idAnna, "Bo", "Hansen", "Vej 2", "b@b.dk", "22222222", true);

        using var tjek = NyContext();
        var carl = await tjek.Volunteers.SingleAsync(v => v.FirstName == "Carl");
        Assert.Equal("Madsen", carl.LastName);
        Assert.Equal("a@a.dk", carl.Email);
        Assert.False(carl.StartedBefore2026);
    }

    // Aktiv-status og registreringer må ikke ændres af en opdatering
    [Fact]
    public async Task UpdateVolunteerAsync_RoererIkkeIsActiveOgSessions()
    {
        var v = new Volunteer("Anna", "Jensen", "Vej 1", "a@a.dk", "11111111");
        v.CheckIn();
        v.Deactivate();
        _db.Volunteers.Add(v);
        await _db.SaveChangesAsync();

        await _service.UpdateVolunteerAsync(v.Id, "Bo", "Hansen", "Vej 2", "b@b.dk", "22222222", false);

        using var tjek = NyContext();
        var gemt = await tjek.Volunteers.Include(x => x.Sessions).SingleAsync();
        Assert.False(gemt.IsActive);
        Assert.Single(gemt.Sessions);
    }

    // Opdatering med samme værdier er ufarlig
    [Fact]
    public async Task UpdateVolunteerAsync_SammeVaerdier_ReturnererTrue()
    {
        var id = await OpretFrivillig();

        var ok = await _service.UpdateVolunteerAsync(id, "Anna", "Jensen", "Vej 1", "a@a.dk", "11111111", false);

        Assert.True(ok);
        using var tjek = NyContext();
        Assert.Equal("Anna", (await tjek.Volunteers.SingleAsync()).FirstName);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}