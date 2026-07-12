using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Armageddon.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTournamentResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TournamentResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DatePlayed = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WinnerName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentResults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TournamentResultEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TournamentResultId = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamName = table.Column<string>(type: "TEXT", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    RoundReached = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentResultEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentResultEntries_TournamentResults_TournamentResultId",
                        column: x => x.TournamentResultId,
                        principalTable: "TournamentResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentResultEntries_TournamentResultId",
                table: "TournamentResultEntries",
                column: "TournamentResultId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TournamentResultEntries");

            migrationBuilder.DropTable(
                name: "TournamentResults");
        }
    }
}
