using DyrenesBeskyttelse.Components.Models;

namespace DyrenesBeskyttelse.Services;

public class AdminSession
{
    public string? FullName { get; private set; }
    public bool IsLoggedIn => FullName is not null;
    public event Action? OnChange;

    public void SignIn(Admin admin)
    {
        FullName = admin.getfullName();
        OnChange?.Invoke();
    }

    public void SignOut()
    {
        FullName = null;
        OnChange?.Invoke();
    }
}