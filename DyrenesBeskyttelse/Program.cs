using DyrenesBeskyttelse.Components;
using DyrenesBeskyttelse.Components.Services;
using DyrenesBeskyttelse.Data;
using DyrenesBeskyttelse.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Authorization; 


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// SQLite-databasen. Forbindelsesstrengen står i appsettings.json, og databasen er én fil
// (dyrenesbeskyttelse.db) i projektmappen.
// AddDbContext registrerer AppDbContext til dependency injection som en scoped service.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// Services er scoped ligesom AppDbContext, så de kan få den injiceret i deres constructor.
// I Blazor Server varer et almindeligt scope lige så længe som browserforbindelsen. Derfor arver
// siderne fra OwningComponentBase, som giver hver komponent sit eget scope: Hver gang en side
// åbnes, laves en ny service og en ny AppDbContext, og de ryddes op, når siden forlades.
// Data deles mellem brugerne via databasen.
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<VolunteerService>();
builder.Services.AddScoped<AdminSession>();

var app = builder.Build();

// Opretter databasefilen og tabellerne ud fra migrationerne (Data/Migrations), hvis de mangler.
// AppDbContext er scoped, så vi laver et scope for at kunne hente den her ved opstart.

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    // Opret en første admin, hvis der ingen er
    if (!await db.Admins.AnyAsync())
    {
        var adminService = scope.ServiceProvider.GetRequiredService<AdminService>();
        await adminService.CreateAdminAsync("Admin", "Admin", "Ukendt vej 1",
            "admin@dyrenesbeskyttelse.dk", "12345678", "Admin123!");
    }

    // Testfrivillige, så listen er længere, når programmet åbnes (se Data/TestData.cs).
    await TestData.SeedVolunteersAsync(db);
    await TestData.SeedAttendanceAsync(db);
    var volunteerService = scope.ServiceProvider
        .GetRequiredService<VolunteerService>();

    await volunteerService.InitializeService();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
