using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CocktailOS.Kiosk.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNetworkAccessSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NetworkAccessEnabled",
                table: "MachineConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NetworkAccessEnabled",
                table: "MachineConfigurations");
        }
    }
}
