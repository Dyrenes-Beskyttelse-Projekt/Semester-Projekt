using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DyrenesBeskyttelse.Migrations
{
    /// <inheritdoc />
    public partial class AddExpiredAtToAttendanceSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiredAt",
                table: "AttendanceSessions",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiredAt",
                table: "AttendanceSessions");
        }
    }
}
