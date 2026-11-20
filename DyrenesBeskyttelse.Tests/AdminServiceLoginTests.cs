using DyrenesBeskyttelse.Components.Services;
using DyrenesBeskyttelse.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DyrenesBeskyttelse.Tests;

// US-4.6 (Julie, 17/11): Unit tests af login i AdminService mod en SQLite-database i hukommelsen.
// Kun de vigtigste tilfælde (acceptkriterierne i US-4):
//   - rigtigt login virker, forkert kodeord og ukendt e-mail afvises ("Forkert brugernavn eller
//     kodeord afvises" - vi logger ind med e-mail),
//   - to administratorer kan ikke have samme e-mail ("hver sin bruger").
// Hver test får sin egen tomme database, ligesom i VolunteerServiceTabletTests.
public class AdminServiceLoginTests : IDisposable
{
    private const string Email = "leder@test.dk";
    private const string Password = "Korrekt123!";

    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly AdminService _service;

    public AdminServiceLoginTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _db = new AppDbContext(options);
        _db.Database.Migrate();

        _service = new AdminService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private Task<Components.Models.Admin> CreateLederAsync() =>
        _service.CreateAdminAsync("Leder", "Test", "Testvej 1", Email, "12345678", Password);

    // Rigtig e-mail og rigtigt kodeord giver adgang.
    [Fact]
    public async Task Login_CorrectEmailAndPassword_Succeeds()
    {
        await CreateLederAsync();

        var result = await _service.LoginAsync(Email, Password);

        Assert.True(result.Success);
        Assert.NotNull(result.Admin);
        Assert.Null(result.ErrorMessage);
    }

    // Forkert kodeord afvises.
    [Fact]
    public async Task Login_WrongPassword_IsRejected()
    {
        await CreateLederAsync();

        var result = await _service.LoginAsync(Email, "Forkert123!");

        Assert.False(result.Success);
        Assert.Null(result.Admin);
    }

    // Ukendt e-mail afvises med SAMME fejlbesked som forkert kodeord, så man ikke kan gætte,
    // hvilke e-mails der findes.
    [Fact]
    public async Task Login_UnknownEmail_IsRejectedWithSameMessage()
    {
        await CreateLederAsync();

        var unknownEmail = await _service.LoginAsync("findes.ikke@test.dk", Password);
        var wrongPassword = await _service.LoginAsync(Email, "Forkert123!");

        Assert.False(unknownEmail.Success);
        Assert.Equal(wrongPassword.ErrorMessage, unknownEmail.ErrorMessage);
    }

    // Lederen og frivilligkoordinatoren har hver sin bruger: to administratorer kan ikke have samme e-mail.
    [Fact]
    public async Task CreateAdmin_SameEmailTwice_IsRejected()
    {
        await CreateLederAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAdminAsync("Anden", "Person", "Testvej 2", Email.ToUpper(), "87654321", "Andet123!"));
    }
}
