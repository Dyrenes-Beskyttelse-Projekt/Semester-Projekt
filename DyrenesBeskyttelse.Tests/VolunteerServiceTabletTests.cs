using DyrenesBeskyttelse.Components.Models;
using DyrenesBeskyttelse.Components.Services;
using DyrenesBeskyttelse.Data;
using DyrenesBeskyttelse.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DyrenesBeskyttelse.Tests;

// Tests af VolunteerService' tablet-metoder (US-1, US-2, US-3) mod en SQLite-database i hukommelsen.
// Databasen findes kun, så længe forbindelsen er åben, så hver test får sin egen tomme database.
// OVERLAP: Metoderne, der testes her, overlapper med Simons (US-1.2, US-1.4, US-3.4) og Jonas'
// (US-2.3, US-3.3) tasks (se kommentaren i VolunteerService). Vælger vi mandag deres udgaver,
// rettes metodenavnene her, og testene bruges til at tjekke, at det hele stadig virker.
public class VolunteerServiceTabletTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly VolunteerService _service;

    public VolunteerServiceTabletTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _db = new AppDbContext(options);
        _db.Database.Migrate();

        _service = new VolunteerService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private long AddVolunteer(string firstName, string lastName, bool isActive = true)
    {
        var volunteer = new Volunteer(firstName, lastName, "Testvej 1", $"{firstName}@test.dk", "12345678");
        // Nye frivillige er aktive fra start, så kun de inaktive skal ændres.
        if (!isActive)
        {
            volunteer.Deactivate();
        }

        _db.Volunteers.Add(volunteer);
        _db.SaveChanges();
        return volunteer.Id;
    }

    // US-1.2 og US-1.3: Kun aktive frivillige, alfabetisk og kun med navn.
    [Fact]
    public async Task GetTabletList_ShowsOnlyActiveVolunteersInAlphabeticalOrder()
    {
        AddVolunteer("Albin", "Meha");
        AddVolunteer("Stoppet", "Frivillig", isActive: false);
        AddVolunteer("Julie", "Bjørn");

        var list = await _service.GetTabletListAsync();

        Assert.Equal(new[] { "Albin Meha", "Julie Bjørn" }, list.Select(v => v.FullName));
        Assert.All(list, v => Assert.False(v.IsCheckedIn));
    }

    // US-2 og US-3.2: Efter tjek ind står den frivillige som tjekket ind på tabletten.
    [Fact]
    public async Task CheckIn_MarksVolunteerAsCheckedIn()
    {
        long id = AddVolunteer("Julie", "Bjørn");

        bool ok = await _service.CheckInAsync(id);
        var volunteer = (await _service.GetTabletListAsync()).Single();

        Assert.True(ok);
        Assert.True(volunteer.IsCheckedIn);
        Assert.True(volunteer.CanCheckOut);
    }

    // US-2: Man kan ikke tjekke ind to gange.
    [Fact]
    public async Task CheckIn_Twice_IsRejected()
    {
        long id = AddVolunteer("Julie", "Bjørn");
        await _service.CheckInAsync(id);

        bool secondTime = await _service.CheckInAsync(id);

        Assert.False(secondTime);
    }

    // US-3.2: Det er kun muligt at tjekke ud, når man er tjekket ind.
    [Fact]
    public async Task CheckOut_WhenNotCheckedIn_IsRejected()
    {
        long id = AddVolunteer("Julie", "Bjørn");

        bool ok = await _service.CheckOutAsync(id);

        Assert.False(ok);
    }

    [Fact]
    public async Task CheckOut_AfterCheckIn_SavesEndTime()
    {
        long id = AddVolunteer("Julie", "Bjørn");
        await _service.CheckInAsync(id);

        bool ok = await _service.CheckOutAsync(id);
        var session = await _db.Attendances.SingleAsync(TestContext.Current.CancellationToken);
        var volunteer = (await _service.GetTabletListAsync()).Single();

        Assert.True(ok);
        Assert.NotNull(session.SessionEnd);
        Assert.False(volunteer.IsCheckedIn);
    }
}
