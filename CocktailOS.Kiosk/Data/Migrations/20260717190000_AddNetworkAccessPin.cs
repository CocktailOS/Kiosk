using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CocktailOS.Kiosk.Data.Migrations
{
    public partial class AddNetworkAccessPin : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NetworkAccessPinHash",
                table: "MachineConfigurations",
                type: "TEXT",
                maxLength: 200,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NetworkAccessPinHash",
                table: "MachineConfigurations");
        }
    }
}
