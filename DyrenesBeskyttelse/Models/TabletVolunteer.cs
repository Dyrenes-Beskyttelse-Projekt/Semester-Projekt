using System.Globalization;

namespace DyrenesBeskyttelse.Components.Models;

// Det eneste, tabletten får at vide om en frivillig (US-1.3): id, navn og hvornår vedkommende
// tjekkede ind i dag. Adresse, telefonnummer og mail kommer aldrig med, fordi de kun må ses
// i administrationsdelen. Derfor bruger tabletten denne record og ikke Volunteer.
public record TabletVolunteer(long Id, string FullName, DateTime? CheckedInAt)
{
    public bool IsCheckedIn => CheckedInAt.HasValue;

    // US-3.2: Man kan kun tjekke ud, når man er tjekket ind, og kun tjekke ind, når man ikke er.
    // Computed properties. Kan i stedet skrives på en simplere måde, men det er mere læsbart at have to properties, da de tydeligt beskriver, hvilke handlinger der er tilladt
    // og gør det nemmere at bruge i Blazor, hvor man kan binde til dem direkte.
    public bool CanCheckOut => IsCheckedIn;
    public bool CanCheckIn => !IsCheckedIn;

    // Dansk sortering, så Æ, Ø og Å kommer til sidst i alfabetet (US-1: navnene vises alfabetisk).
    private static readonly StringComparer DanishOrder =
        StringComparer.Create(new CultureInfo("da-DK"), ignoreCase: true);

    public static List<TabletVolunteer> SortByName(IEnumerable<TabletVolunteer> volunteers) =>
        volunteers.OrderBy(v => v.FullName, DanishOrder).ToList();

    public static IEnumerable<TabletVolunteer> SearchByName(
        IEnumerable<TabletVolunteer> volunteers, string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return volunteers;
        
        return volunteers.Where(v => v.FullName
            .Contains(searchText.Trim(), StringComparison.OrdinalIgnoreCase));
    }

}
