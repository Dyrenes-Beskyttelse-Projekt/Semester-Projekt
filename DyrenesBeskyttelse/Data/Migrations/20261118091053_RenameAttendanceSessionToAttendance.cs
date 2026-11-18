using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DyrenesBeskyttelse.Data.Migrations
{
    // 18/11: AttendanceSession er omdøbt til Attendance (så den ikke forveksles med AdminSession).
    // EF foreslog at slette tabellen og oprette en ny, men så ville alle registreringer af fremmøde
    // forsvinde. Derfor omdøbes tabellen og indekset i stedet, så data bevares.
    /// <inheritdoc />
    public partial class RenameAttendanceSessionToAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "AttendanceSessions",
                newName: "Attendances");

            migrationBuilder.RenameIndex(
                name: "IX_AttendanceSessions_VolunteerId",
                table: "Attendances",
                newName: "IX_Attendances_VolunteerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_Attendances_VolunteerId",
                table: "Attendances",
                newName: "IX_AttendanceSessions_VolunteerId");

            migrationBuilder.RenameTable(
                name: "Attendances",
                newName: "AttendanceSessions");
        }
    }
}
