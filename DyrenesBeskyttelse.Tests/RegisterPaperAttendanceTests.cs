using DyrenesBeskyttelse.Components.Models;
using DyrenesBeskyttelse.Components.Services;
using DyrenesBeskyttelse.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DyrenesBeskyttelse.Tests;

// US-10.1: Tests af registrering af fremmøde fra papirlisten
public class RegisterPaperAttendanceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly AdminService _service;

    public RegisterPaperAttendanceTests()
    {
        // SQLite i hukommelsen: forbindelsen skal holdes åben, ellers forsvinder databasen
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
        _service = new AdminService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private async Task<Volunteer> CreateVolunteerAsync()
    {
        var volunteer = new Volunteer("Test", "Person", "Vej 1", "test@test.dk", "12345678");
        _db.Volunteers.Add(volunteer);
        await _db.SaveChangesAsync();
        return volunteer;
    }

    [Fact]
    public async Task RegisterPaperAttendance_SavesAttendanceOnGivenDate()
    {
        var volunteer = await CreateVolunteerAsync();
        var date = new DateTime(2026, 10, 5);

        var ok = await _service.RegisterPaperAttendanceAsync(volunteer.Id, date);

        Assert.Equal(PaperAttendanceResult.Registered, ok);
        var attendance = await _db.Attendances.SingleAsync();
        Assert.Equal(date, attendance.SessionStart);
        Assert.Equal(volunteer.Id, _db.Entry(attendance).Property<long?>("VolunteerId").CurrentValue);
    }

    [Fact]
    public async Task RegisterPaperAttendance_SetsSessionEnd_SoItIsNotCheckedInNow()
    {
        var volunteer = await CreateVolunteerAsync();

        await _service.RegisterPaperAttendanceAsync(volunteer.Id, new DateTime(2026, 10, 5));

        var attendance = await _db.Attendances.SingleAsync();
        Assert.NotNull(attendance.SessionEnd);
        Assert.False(attendance.IsActive);
        Assert.Empty(await _db.Attendances.Where(a => a.SessionEnd == null).ToListAsync());
    }

    [Fact]
    public async Task RegisterPaperAttendance_UnknownVolunteer_ReturnsFalseAndSavesNothing()
    {
        var ok = await _service.RegisterPaperAttendanceAsync(999, new DateTime(2026, 10, 5));

        Assert.Equal(PaperAttendanceResult.VolunteerNotFound, ok);
        Assert.Empty(await _db.Attendances.ToListAsync());
    }
    
    [Fact]
    public async Task RegisterPaperAttendance_AlreadyCheckedIn_ReturnsAlreadyCheckedIn_AndSavesNothing()
    {
        var volunteer = await CreateVolunteerAsync();

        // Åben session: SessionEnd er null, som når man er tjekket ind på tabletten
        volunteer.Sessions.Add(new Attendance(0, new DateTime(2026, 10, 4)));
        await _db.SaveChangesAsync();

        var result = await _service.RegisterPaperAttendanceAsync(volunteer.Id, new DateTime(2026, 10, 5));

        Assert.Equal(PaperAttendanceResult.AlreadyCheckedIn, result);
        // Kun den åbne session fra Arrange må findes, der er ikke tilføjet en ny
        Assert.Single(await _db.Attendances.ToListAsync());
    }
}