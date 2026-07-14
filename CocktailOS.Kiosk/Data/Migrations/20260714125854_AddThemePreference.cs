using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CocktailOS.Kiosk.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddThemePreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Theme",
                table: "MachineConfigurations",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "Dark");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Theme",
                table: "MachineConfigurations");
        }
    }
}
