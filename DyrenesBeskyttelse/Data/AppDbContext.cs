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
    public DbSet<Attendance> Attendances => Set<Attendance>();

    // Timesheet og TimesheetReport er ikke tabeller. De kan beregnes ud fra Attendances,
    // når der skal laves en rapport.
    
    
    //Unikt E-mail for Admin
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Admin>()
            .HasIndex(a => a.Email)
            .IsUnique();
    }
}
