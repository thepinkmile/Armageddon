using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Armageddon.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTournamentResultEntryTotalPoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TotalPoints",
                table: "TournamentResultEntries",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalPoints",
                table: "TournamentResultEntries");
        }
    }
}
