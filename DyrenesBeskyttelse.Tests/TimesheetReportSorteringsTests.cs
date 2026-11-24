using DyrenesBeskyttelse.Components.Models;
using DyrenesBeskyttelse.Models;
using Xunit;

namespace DyrenesBeskyttelse.Tests;

// US-8.9: Unit test af sorteringen i US-8.8
public class TimesheetReportSortingTests
{
    // Laver et timesheet med fremmøde på de givne dage i november 2026 (én session pr. dag).
    private static Timesheet Lav(string navn, bool bryder, params int[] dage)
    {
        var sessions = dage
            .Select(d => new Attendance(0, new DateTime(2026, 11, d, 10, 0, 0)))
            .ToList();

        // 24/11: IsActive = true, fordi rapporten kun viser aktive frivillige (US-8.10).
        return new Timesheet(navn, sessions) { BreaksActivityRule = bryder, IsActive = true };
    }

    private static TimesheetReport Rapport(params Timesheet[] timesheets)
    {
        return new TimesheetReport(new DateTime(2026, 11, 1), new DateTime(2026, 11, 30), timesheets.ToList());
    }

    [Fact]
    public void Sorted_RuleBreakersComeBeforeThoseWhoFollowTheRule()
    {
        var report = Rapport(
            Lav("Overholder", false),
            Lav("Bryder", true, 1, 2, 3));

        var result = report.GetSortedTimesheets();

        Assert.Equal("Bryder", result[0].Name);
        Assert.Equal("Overholder", result[1].Name);
    }

    [Fact]
    public void Sorted_AmongBreakers_FewestDaysFirst()
    {
        var report = Rapport(
            Lav("Tre", true, 1, 2, 3),
            Lav("Nul", true),
            Lav("To", true, 1, 2),
            Lav("En", true, 1));

        var names = report.GetSortedTimesheets().Select(t => t.Name).ToList();

        Assert.Equal(new[] { "Nul", "En", "To", "Tre" }, names);
    }

    [Fact]
    public void Sorted_SameNumberOfDays_AreSortedAlphabetically()
    {
        var report = Rapport(
            Lav("Simon", true),
            Lav("Albin", true),
            Lav("Julie", true));

        var names = report.GetSortedTimesheets().Select(t => t.Name).ToList();

        Assert.Equal(new[] { "Albin", "Julie", "Simon" }, names);
    }

    [Fact]
    public void Sorted_TwoSessionsOnSameDay_CountAsOneDay()
    {
        // Mike har 3 tjek ind, men kun på én dag. Benn har 2 forskellige dage. Mike skal stå først.
        var mike = new Timesheet("Mike", new List<Attendance>
        {
            new(0, new DateTime(2026, 11, 16, 8, 0, 0)),
            new(0, new DateTime(2026, 11, 16, 12, 0, 0)),
            new(0, new DateTime(2026, 11, 16, 16, 0, 0))
        }) { BreaksActivityRule = true, IsActive = true };

        var benn = Lav("Benn", true, 6, 13);

        var result = Rapport(benn, mike).GetSortedTimesheets();

        Assert.Equal("Mike", result[0].Name);
        Assert.Equal("Benn", result[1].Name);
    }

    [Fact]
    public void Sorted_FullExample_BreakersFirstThenTheRest()
    {
        var report = Rapport(
            Lav("Hanne", false, 1, 13),
            Lav("Benn", true, 6, 13),
            Lav("Albin", true),
            Lav("Klaus", false, 4, 15),
            Lav("Mike", true, 16));

        var names = report.GetSortedTimesheets().Select(t => t.Name).ToList();

        Assert.Equal(new[] { "Albin", "Mike", "Benn", "Hanne", "Klaus" }, names);
    }

    [Fact]
    public void Sorted_DoesNotChangeOriginalList()
    {
        var report = Rapport(Lav("B", false), Lav("A", true));

        report.GetSortedTimesheets();

        Assert.Equal("B", report.Timesheets[0].Name);
    }

    [Fact]
    public void Sorted_EmptyReport_ReturnsEmptyList()
    {
        Assert.Empty(Rapport().GetSortedTimesheets());
    }

    [Fact]
    public void CountBreakingRule_CountsOnlyBreakers()
    {
        var report = Rapport(
            Lav("A", true),
            Lav("B", false),
            Lav("C", true),
            Lav("D", false));

        Assert.Equal(2, report.CountBreakingRule());
    }
}