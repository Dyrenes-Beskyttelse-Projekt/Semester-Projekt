using DyrenesBeskyttelse.Components.Models;
using Xunit.Internal;

namespace DyrenesBeskyttelse.Tests;

// Unit tests af det, tabletten viser (US-1.3 og US-3.2). Ingen database.
public class TabletVolunteerTests
{
    private readonly TabletVolunteer[] _volunteers =
    {
        new TabletVolunteer(1, "Åse Holm", null),
        new TabletVolunteer(2, "Æsbjørn Nielsen", null),
        new TabletVolunteer(3, "Albin Meha", null),
        new TabletVolunteer(4, "Simon Stensgaard", null),
        new TabletVolunteer(5, "Julie Bjørn", null),
        new TabletVolunteer(6, "Klaus Valle", null),
    };

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
        var names = TabletVolunteer.SortByName(_volunteers).Select(v => v.FullName);
        Assert.Equal(
            new[] { "Albin Meha", "Julie Bjørn", "Klaus Valle", "Simon Stensgaard", "Æsbjørn Nielsen", "Åse Holm" },
            names);
    }

    // US-17.2: Unit tests af søgningen, fx at ”kla” finder Klaus, og at et tomt felt viser alle.
    [Fact]
    public void SearchByName_PartialName_FindsMatchingVolunteer()
    {
        // Act
        var result = TabletVolunteer.SearchByName(_volunteers, "kla");
        
        // Assert
        Assert.Equal(new[] { "Klaus Valle" }, result.Select(v => v.FullName));
    }
    
    [Fact]
    public void SearchByName_UpperCaseSearchText_FindsVolunteer()
    {
        // Act
        var result = TabletVolunteer.SearchByName(_volunteers, "KLA");

        // Assert
        Assert.Equal(new[] { "Klaus Valle" }, result.Select(v => v.FullName));
    }
    [Fact]
    public void SearchByName_EmptyName_FindsAllVolunteers()
    {
        // Act
        var resultAllEmpty = TabletVolunteer.SearchByName(_volunteers, "");

        // Assert
        Assert.Equal(_volunteers.Select(v => v.FullName), resultAllEmpty.Select(v => v.FullName));
    }

    [Fact]
    public void SearchByName_Null_FindsAllVolunteers()
    {
        // Act
        var resultAllNull = TabletVolunteer.SearchByName(_volunteers, null);
        // Assert
        Assert.Equal(_volunteers.Select(v => v.FullName), resultAllNull.Select(v => v.FullName));
    }
    
    [Fact]
    public void SearchByName_NoMatches_ReturnsEmptyList()
    {
        // Act
        var result = TabletVolunteer.SearchByName(_volunteers, "XyZ");
        // Assert
        Assert.Empty(result);
    }
}