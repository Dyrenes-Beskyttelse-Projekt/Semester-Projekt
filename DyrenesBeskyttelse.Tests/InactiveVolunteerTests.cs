using DyrenesBeskyttelse.Components.Models;
using DyrenesBeskyttelse.Components.Services;
using DyrenesBeskyttelse.Data;
using DyrenesBeskyttelse.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DyrenesBeskyttelse.Tests;

// Albin (US-7.2): Nye frivillige er aktive fra start, og inaktive forsvinder fra tabletten,
// mens deres registreringer (Attendances) bevares. Kan gøres aktive igen (US-7).
public class InactiveVolunteerTests
{
    // Laver en tom database i hukommelsen til hver test.
    private static (AppDbContext db, SqliteConnection conn) CreateDb()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(conn)
            .Options;

        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return (db, conn);
    }

    private static Volunteer NewVolunteer()
    {
        return new Volunteer("Anna", "Hansen", "Vej 1", "anna@test.dk", "12345678");
    }

    [Fact]
    public void NewVolunteer_IsActiveFromStart()
    {
        var volunteer = NewVolunteer();

        Assert.True(volunteer.IsActive);
    }

    [Fact]
    public async Task NewVolunteerIsShownOnTablet()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;

        db.Volunteers.Add(NewVolunteer());
        await db.SaveChangesAsync();

        var service = new VolunteerService(db);
        var list = await service.GetTabletListAsync();

        Assert.Single(list);
    }

    [Fact]
    public async Task InactiveVolunteerIsHiddenFromTablet()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;

        var volunteer = NewVolunteer();
        db.Volunteers.Add(volunteer);
        await db.SaveChangesAsync();

        volunteer.Deactivate();
        await db.SaveChangesAsync();

        var service = new VolunteerService(db);
        var list = await service.GetTabletListAsync();

        Assert.Empty(list);
    }

    [Fact]
    public async Task InactiveVolunteerKeepsAttendanceSessions()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;

        var volunteer = NewVolunteer();
        volunteer.CheckIn();
        db.Volunteers.Add(volunteer);
        await db.SaveChangesAsync();

        volunteer.Deactivate();
        await db.SaveChangesAsync();

        Assert.Equal(1, await db.Attendances.CountAsync());
    }

    [Fact]
    public async Task InactiveVolunteer_ReturnsToTabletWhenActivatedAgain()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;

        var volunteer = NewVolunteer();
        db.Volunteers.Add(volunteer);
        await db.SaveChangesAsync();

        var service = new VolunteerService(db);

        volunteer.Deactivate();
        await db.SaveChangesAsync();
        Assert.Empty(await service.GetTabletListAsync());

        volunteer.Activate();
        await db.SaveChangesAsync();
        Assert.Single(await service.GetTabletListAsync());
    }

    [Fact]
    public async Task InactiveVolunteerCannotCheckIn()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;

        var volunteer = NewVolunteer();
        volunteer.Deactivate();
        db.Volunteers.Add(volunteer);
        await db.SaveChangesAsync();

        var service = new VolunteerService(db);
        var ok = await service.CheckInAsync(volunteer.Id);

        Assert.False(ok);
    }
}
