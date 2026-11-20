using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DyrenesBeskyttelse.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStartedBefore2026 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "StartedBefore2026",
                table: "Volunteers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StartedBefore2026",
                table: "Volunteers");
        }
    }
}
