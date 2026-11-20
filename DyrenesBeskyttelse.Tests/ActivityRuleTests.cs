using DyrenesBeskyttelse.Components.Models;

namespace DyrenesBeskyttelse.Tests;

// US-9.2 (Mikes task, lavet af Julie d. 17/11): Unit tests af aktivitetsreglen (ActivityRule).
// Ingen database. Kun de vigtigste grænsetilfælde fra tasken: 13/14 og 27/28 dage samt 0 og 1
// fremmøde i måneden. Alle tests bruger november 2026 (30 dage).
public class ActivityRuleTests
{
    private static DateTime Nov(int day) => new DateTime(2026, 11, day);

    // Ny frivillig: 13 dage mellem to fremmøder er lige præcis tilladt.
    [Fact]
    public void New_Gap13Days_Ok()
    {
        // 1/11 -> 14/11 = 13 dage, 14/11 -> 27/11 = 13 dage, 27/11 -> 30/11 = 3 dage.
        var dates = new[] { Nov(1), Nov(14), Nov(27) };

        Assert.False(ActivityRule.BreaksRule(false, dates, 2026, 11));
    }

    // Ny frivillig: 14 dage er én dag for meget.
    [Fact]
    public void New_Gap14Days_Breaks()
    {
        // 1/11 -> 15/11 = 14 dage.
        var dates = new[] { Nov(1), Nov(15), Nov(28) };

        Assert.True(ActivityRule.BreaksRule(false, dates, 2026, 11));
    }

    // Gammel frivillig: 27 dage er lige præcis tilladt.
    [Fact]
    public void Old_Gap27Days_Ok()
    {
        // 1/11 -> 28/11 = 27 dage, 28/11 -> 30/11 = 2 dage.
        var dates = new[] { Nov(1), Nov(28) };

        Assert.False(ActivityRule.BreaksRule(true, dates, 2026, 11));
    }

    // Gammel frivillig: 28 dage er én dag for meget.
    [Fact]
    public void Old_Gap28Days_Breaks()
    {
        // 1/11 -> 29/11 = 28 dage.
        var dates = new[] { Nov(1), Nov(29) };

        Assert.True(ActivityRule.BreaksRule(true, dates, 2026, 11));
    }

    // 0 fremmøder i måneden: altid markeret, både gamle og nye.
    [Fact]
    public void NoAttendance_AlwaysBreaks()
    {
        var dates = Array.Empty<DateTime>();

        Assert.True(ActivityRule.BreaksRule(true, dates, 2026, 11));
        Assert.True(ActivityRule.BreaksRule(false, dates, 2026, 11));
    }

    // 1 fremmøde midt i måneden: tiden fra månedens start og til månedens slutning tæller med.
    [Fact]
    public void OneAttendance_MidMonth_OldOkNewBreaks()
    {
        // 1/11 -> 15/11 = 14 dage, 15/11 -> 30/11 = 15 dage.
        var dates = new[] { Nov(15) };

        Assert.False(ActivityRule.BreaksRule(true, dates, 2026, 11));   // 15 <= 27
        Assert.True(ActivityRule.BreaksRule(false, dates, 2026, 11));   // 15 > 13
    }
}
