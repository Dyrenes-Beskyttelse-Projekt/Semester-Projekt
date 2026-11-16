using DyrenesBeskyttelse.Components.Models;

namespace DyrenesBeskyttelse.Tests;

// Unit tests af det, tabletten viser (US-1.3 og US-3.2). Ingen database.
public class TabletVolunteerTests
{
    // US-1.3: Tabletten må kun kende id, navn og status, aldrig adresse, telefon eller mail.
    [Fact]
    public void TabletVolunteer_HasNoContactInformation()
    {
        var properties = typeof(TabletVolunteer).GetProperties().Select(p => p.Name).ToHashSet();

        Assert.DoesNotContain("Address", properties);
        Assert.DoesNotContain("Email", properties);
        Assert.DoesNotContain("PhoneNumber", properties);
        Assert.Equal(
            new HashSet<string> { "Id", "FullName", "CheckedInAt", "IsCheckedIn", "CanCheckIn", "CanCheckOut" },
            properties);
    }

    // US-3.2: Man kan kun tjekke ud, når man er tjekket ind.
    [Fact]
    public void NotCheckedIn_CanCheckInButNotOut()
    {
        var volunteer = new TabletVolunteer(1, "Julie Bjørn", CheckedInAt: null);

        Assert.False(volunteer.IsCheckedIn);
        Assert.True(volunteer.CanCheckIn);
        Assert.False(volunteer.CanCheckOut);
    }

    [Fact]
    public void CheckedIn_CanCheckOutButNotIn()
    {
        var volunteer = new TabletVolunteer(1, "Julie Bjørn", CheckedInAt: DateTime.Today.AddHours(9));

        Assert.True(volunteer.IsCheckedIn);
        Assert.True(volunteer.CanCheckOut);
        Assert.False(volunteer.CanCheckIn);
    }

    // US-1: Navnene vises alfabetisk, og Æ, Ø og Å kommer til sidst som i det danske alfabet.
    [Fact]
    public void SortByName_UsesDanishAlphabet()
    {
        var volunteers = new[]
        {
            new TabletVolunteer(1, "Åse Holm", null),
            new TabletVolunteer(2, "Æsbjørn Nielsen", null),
            new TabletVolunteer(3, "Albin Meha", null),
            new TabletVolunteer(4, "Simon Stensgaard", null),
            new TabletVolunteer(5, "Julie Bjørn", null),
        };

        var names = TabletVolunteer.SortByName(volunteers).Select(v => v.FullName);
        Assert.Equal(new[] { "Albin Meha", "Julie Bjørn", "Simon Stensgaard", "Æsbjørn Nielsen", "Åse Holm" }, names);
    }
}
