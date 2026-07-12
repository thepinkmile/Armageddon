using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Armageddon.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchIsPlayoff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPlayoff",
                table: "Matches",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPlayoff",
                table: "Matches");
        }
    }
}
