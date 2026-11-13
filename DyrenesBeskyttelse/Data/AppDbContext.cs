using DyrenesBeskyttelse.Components.Models;
using Microsoft.EntityFrameworkCore;

namespace DyrenesBeskyttelse.Data;

// EF Core-konteksten. Hver DbSet bliver en tabel i SQLite-databasen.
// Forbindelsen (hvilken databasefil) sættes i Program.cs ud fra appsettings.json.
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Volunteer> Volunteers => Set<Volunteer>();
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<AttendanceSession> AttendanceSessions => Set<AttendanceSession>();

    // Timesheet og TimesheetReport er ikke tabeller. De kan beregnes ud fra AttendanceSessions,
    // når der skal laves en rapport.
}
