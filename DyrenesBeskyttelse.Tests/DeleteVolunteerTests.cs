using DyrenesBeskyttelse.Components.Models;
using DyrenesBeskyttelse.Components.Services;
using DyrenesBeskyttelse.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DyrenesBeskyttelse.Tests;

// US-19.3: Unit tests af AdminService.DeleteVolunteerAsync (US-19.1).
// En aktiv frivillig kan ikke slettes, og en inaktiv slettes sammen med sine registreringer.
public class DeleteVolunteerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly AppDbContext _db;
    private readonly AdminService _service;

    public DeleteVolunteerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _db = new AppDbContext(_options);
        _db.Database.EnsureCreated();
        _service = new AdminService(_db);
    }

    // Opretter en frivillig med én registrering af fremmøde.
    private async Task<long> OpretFrivillig(bool aktiv)
    {
        var v = new Volunteer("Anna", "Jensen", "Vej 1", "a@a.dk", "11111111");
        var tid = new DateTime(2026, 10, 5, 10, 0, 0);
        v.Sessions.Add(new Attendance(0, tid) { SessionEnd = tid.AddHours(2) });
        if (!aktiv) v.Deactivate();
        _db.Volunteers.Add(v);
        await _db.SaveChangesAsync();
        return v.Id;
    }

    // Læser med en NY context, så vi ved, hvad der faktisk står i databasen.
    private AppDbContext NyContext() => new(_options);

    [Fact]
    public async Task AktivFrivillig_KanIkkeSlettes()
    {
        var id = await OpretFrivillig(aktiv: true);

        var ok = await _service.DeleteVolunteerAsync(id);

        Assert.False(ok);
        using var tjek = NyContext();
        Assert.Single(tjek.Volunteers);
        Assert.Single(tjek.Attendances);
    }

    [Fact]
    public async Task InaktivFrivillig_SlettesSammenMedRegistreringerne()
    {
        var id = await OpretFrivillig(aktiv: false);

        var ok = await _service.DeleteVolunteerAsync(id);

        Assert.True(ok);
        using var tjek = NyContext();
        Assert.Empty(tjek.Volunteers);
        Assert.Empty(tjek.Attendances);
    }

    [Fact]
    public async Task UkendtId_ReturnererFalse()
    {
        Assert.False(await _service.DeleteVolunteerAsync(999));
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
