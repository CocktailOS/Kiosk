using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CocktailOS.Kiosk.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPumpCalibration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CalibratedIngredientId",
                table: "Pumps",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FlowRateSource",
                table: "Pumps",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "manual");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastCalibratedAt",
                table: "Pumps",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pumps_CalibratedIngredientId",
                table: "Pumps",
                column: "CalibratedIngredientId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pumps_Ingredients_CalibratedIngredientId",
                table: "Pumps",
                column: "CalibratedIngredientId",
                principalTable: "Ingredients",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pumps_Ingredients_CalibratedIngredientId",
                table: "Pumps");

            migrationBuilder.DropIndex(
                name: "IX_Pumps_CalibratedIngredientId",
                table: "Pumps");

            migrationBuilder.DropColumn(
                name: "CalibratedIngredientId",
                table: "Pumps");

            migrationBuilder.DropColumn(
                name: "FlowRateSource",
                table: "Pumps");

            migrationBuilder.DropColumn(
                name: "LastCalibratedAt",
                table: "Pumps");
        }
    }
}
