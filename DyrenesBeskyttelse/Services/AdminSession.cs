using DyrenesBeskyttelse.Components.Models;

namespace DyrenesBeskyttelse.Services;
using DyrenesBeskyttelse.Components.Models;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;



public class AdminSession
{
    private const string Key = "adminName";
    private readonly ProtectedSessionStorage _storage;
    private bool _restored;

    public AdminSession(ProtectedSessionStorage storage)
    {
        _storage = storage;
    }

    public string? FullName { get; private set; }
    public bool IsLoggedIn => FullName is not null;
    public event Action? OnChange;

    public void SignIn(Admin admin)
    {
        FullName = admin.getfullName();
        _ = _storage.SetAsync(Key, FullName);   // husk login i browseren
        OnChange?.Invoke();
    }

    public void SignOut()
    {
        FullName = null;
        _ = _storage.DeleteAsync(Key);
        OnChange?.Invoke();
    }

    // Gendan login efter genindlæsning
    public async Task RestoreAsync()
    {
        if (_restored) return;
        _restored = true;

        try
        {
            var result = await _storage.GetAsync<string>(Key);
            if (result.Success && result.Value is not null)
            {
                FullName = result.Value;
                OnChange?.Invoke();
            }
        }
        catch
        {
            // Ulæselig værdi: behandl som logget ud
        }
    }
}