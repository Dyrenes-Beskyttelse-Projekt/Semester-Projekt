using DyrenesBeskyttelse.Components.Models;
using DyrenesBeskyttelse.Components.Services;
using DyrenesBeskyttelse.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DyrenesBeskyttelse.Tests;

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
    public async Task NewVolunteer_IsShownOnTablet()
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
    public async Task InactiveVolunteer_IsHiddenFromTablet()
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
    public async Task InactiveVolunteer_KeepsAttendanceSessions()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;

        var volunteer = NewVolunteer();
        volunteer.CheckIn();
        db.Volunteers.Add(volunteer);
        await db.SaveChangesAsync();

        volunteer.Deactivate();
        await db.SaveChangesAsync();

        Assert.Equal(1, await db.AttendanceSessions.CountAsync());
    }

    [Fact]
    public async Task InactiveVolunteer_ReturnsToTablet_WhenActivatedAgain()
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
    public async Task InactiveVolunteer_CannotCheckIn()
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