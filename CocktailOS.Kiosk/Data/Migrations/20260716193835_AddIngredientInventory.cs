using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CocktailOS.Kiosk.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIngredientInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BottleSizeMl",
                table: "Ingredients",
                type: "TEXT",
                precision: 8,
                scale: 2,
                nullable: false,
                defaultValue: 1000m);

            migrationBuilder.AddColumn<decimal>(
                name: "RemainingVolumeMl",
                table: "Ingredients",
                type: "TEXT",
                precision: 8,
                scale: 2,
                nullable: false,
                defaultValue: 1000m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BottleSizeMl",
                table: "Ingredients");

            migrationBuilder.DropColumn(
                name: "RemainingVolumeMl",
                table: "Ingredients");
        }
    }
}
