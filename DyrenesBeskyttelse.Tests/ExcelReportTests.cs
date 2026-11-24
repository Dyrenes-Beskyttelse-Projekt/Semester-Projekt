using ClosedXML.Excel;
using DyrenesBeskyttelse.Components.Models;
using DyrenesBeskyttelse.Models;
using DyrenesBeskyttelse.Services;
using Xunit;

namespace DyrenesBeskyttelse.Tests;

// US-13.2: Unit test af Excel-filen: én række pr. aktiv frivillig med de rigtige datoer,
// i samme rækkefølge som rapporten på skærmen (brud på aktivitetsreglen øverst).
public class ExcelReportTests
{
    private static Timesheet Lav(string navn, bool bryder, bool aktiv, params int[] dage)
    {
        var sessions = dage.Select(d => new Attendance(0, new DateTime(2026, 10, d, 10, 0, 0))).ToList();
        return new Timesheet(navn, sessions)
        {
            BreaksActivityRule = bryder,
            IsActive = aktiv,
            RuleText = "Mindst én gang om ugen"
        };
    }

    // GenerateExcelReport bruger ikke databasen, så ReportService kan laves uden AppDbContext.
    private static IXLWorksheet LavExcel(params Timesheet[] timesheets)
    {
        var report = new TimesheetReport(new DateTime(2026, 10, 1), new DateTime(2026, 10, 31), timesheets.ToList());
        var bytes = new ReportService(null!).GenerateExcelReport(report);
        return new XLWorkbook(new MemoryStream(bytes)).Worksheet(1);
    }

    [Fact]
    public void Excel_HarSammeKolonnerOgRaekkefoelgeSomRapporten()
    {
        var ark = LavExcel(
            Lav("Benn Christensen", false, true, 6, 13, 20, 27),
            Lav("Kent Hansen", true, true));

        // Overskrifterne står i række 4, og de frivillige fra række 5.
        Assert.Equal("Navn", ark.Cell(4, 1).GetString());
        Assert.Equal("Fremmøde", ark.Cell(4, 2).GetString());
        Assert.Equal("Aktivitetsregel", ark.Cell(4, 3).GetString());

        Assert.Equal("Kent Hansen", ark.Cell(5, 1).GetString());
        Assert.Equal("0 dage", ark.Cell(5, 2).GetString());
        Assert.StartsWith("overholdes ikke", ark.Cell(5, 3).GetString());

        Assert.Equal("Benn Christensen", ark.Cell(6, 1).GetString());
        Assert.Equal("06/10, 13/10, 20/10, 27/10", ark.Cell(6, 2).GetString());
    }

    [Fact]
    public void Excel_InaktiveErIkkeMed()
    {
        var ark = LavExcel(
            Lav("Aktiv Person", true, true),
            Lav("Inaktiv Person", true, false));

        Assert.Equal("Aktiv Person", ark.Cell(5, 1).GetString());
        Assert.True(ark.Cell(6, 1).IsEmpty());
    }
}
