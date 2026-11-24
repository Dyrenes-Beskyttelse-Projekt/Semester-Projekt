using DyrenesBeskyttelse.Components.Models;
using DyrenesBeskyttelse.Data;
using DyrenesBeskyttelse.Models;
using DyrenesBeskyttelse.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DyrenesBeskyttelse.Tests;

// US-8.5: Unit tests af månedsrapporten (ReportService) mod en SQLite-database i hukommelsen.
// Alle tests bruger oktober 2026, ligesom testdataene (en hel måned).
public class ReportServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly ReportService _service;

    private static readonly DateTime Start = new(2026, 10, 1);
    private static readonly DateTime Slut = new(2026, 10, 31);

    public ReportServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
        _service = new ReportService(_db);
    }

    // Opretter en frivillig med fremmøde på de givne tidspunkter (tjekket ud igen samme dag).
    private async Task<Volunteer> OpretFrivillig(string fornavn, params DateTime[] fremmoede)
    {
        var v = new Volunteer(fornavn, "Jensen", "Vej 1", "a@a.dk", "11111111");
        foreach (var tid in fremmoede)
        {
            v.Sessions.Add(new Attendance(0, tid) { SessionEnd = tid.AddHours(2) });
        }
        _db.Volunteers.Add(v);
        await _db.SaveChangesAsync();
        return v;
    }

    private async Task<Timesheet> HentTimesheet(string navn)
    {
        var report = await _service.GetReport(Start, Slut);
        return report.GetSortedTimesheets().Single(t => t.Name == navn);
    }

    [Fact]
    public async Task IngenFremmoede_VisesMed0Dage_OgBryderReglen()
    {
        await OpretFrivillig("Anna");

        var t = await HentTimesheet("Anna Jensen");

        Assert.Equal("0 dage", ReportService.AttendanceText(t));
        Assert.True(t.BreaksActivityRule);
    }

    [Fact]
    public async Task ToTjekIndSammeDag_DatoenVisesEnGang()
    {
        await OpretFrivillig("Anna", new DateTime(2026, 10, 5, 9, 0, 0), new DateTime(2026, 10, 5, 14, 0, 0));

        var t = await HentTimesheet("Anna Jensen");

        Assert.Equal("05/10", ReportService.AttendanceText(t));
    }

    // Grænsen ved månedsskiftet: d. 30/9 og 1/11 hører ikke til oktober, men d. 1/10 og 31/10 gør.
    [Fact]
    public async Task FremmoedeVedMaanedsskiftet_KunOktoberKommerMed()
    {
        await OpretFrivillig("Anna",
            new DateTime(2026, 9, 30, 10, 0, 0),
            new DateTime(2026, 10, 1, 10, 0, 0),
            new DateTime(2026, 10, 31, 10, 0, 0),
            new DateTime(2026, 11, 1, 10, 0, 0));

        var t = await HentTimesheet("Anna Jensen");

        Assert.Equal("01/10, 31/10", ReportService.AttendanceText(t));
    }

    // US-8.10: Inaktive vises ikke og tælles ikke med i "X af Y overholder ikke aktivitetsreglen".
    [Fact]
    public async Task InaktivFrivillig_ErIkkeMedIRapporten()
    {
        await OpretFrivillig("Aktiv");
        var inaktiv = await OpretFrivillig("Inaktiv");
        inaktiv.Deactivate();
        await _db.SaveChangesAsync();

        var report = await _service.GetReport(Start, Slut);

        Assert.Equal(new[] { "Aktiv Jensen" }, report.GetSortedTimesheets().Select(t => t.Name));
        Assert.Equal(1, report.CountBreakingRule());
    }

    // US-8.1: Rullemenuen. D. 24/11 er november ikke slut, så oktober står øverst.
    [Fact]
    public void GetCompletedMonths_MidtIMaaneden_SidsteMaanedOeverst()
    {
        var maaneder = ReportService.GetCompletedMonths(new DateTime(2026, 11, 24));

        Assert.Equal(12, maaneder.Count);
        Assert.Equal(new DateTime(2026, 10, 1), maaneder.First());
        Assert.Equal(new DateTime(2025, 11, 1), maaneder.Last());
    }

    // US-8.1: Årsskiftet. D. 1. januar 2027 skal december 2026 stå øverst.
    [Fact]
    public void GetCompletedMonths_1Januar_December2026Oeverst()
    {
        var maaneder = ReportService.GetCompletedMonths(new DateTime(2027, 1, 1));

        Assert.Equal(new DateTime(2026, 12, 1), maaneder.First());
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
