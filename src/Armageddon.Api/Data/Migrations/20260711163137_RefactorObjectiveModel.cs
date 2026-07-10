using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Armageddon.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorObjectiveModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Objectives",
                newName: "Points");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Points",
                table: "Objectives",
                newName: "Type");
        }
    }
}
