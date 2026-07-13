using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodLens.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DietGoals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeviceId = table.Column<string>(type: "TEXT", nullable: false),
                    DailyCalorieLimit = table.Column<double>(type: "REAL", nullable: false),
                    ProteinPercentage = table.Column<double>(type: "REAL", nullable: false),
                    CarbsPercentage = table.Column<double>(type: "REAL", nullable: false),
                    FatPercentage = table.Column<double>(type: "REAL", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DietGoals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FoodCache",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FdcId = table.Column<string>(type: "TEXT", nullable: false),
                    FoodName = table.Column<string>(type: "TEXT", nullable: false),
                    CaloriesPer100g = table.Column<double>(type: "REAL", nullable: false),
                    ProteinPer100g = table.Column<double>(type: "REAL", nullable: false),
                    CarbsPer100g = table.Column<double>(type: "REAL", nullable: false),
                    FatPer100g = table.Column<double>(type: "REAL", nullable: false),
                    CachedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodCache", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FoodLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeviceId = table.Column<string>(type: "TEXT", nullable: false),
                    FoodName = table.Column<string>(type: "TEXT", nullable: false),
                    FdcId = table.Column<string>(type: "TEXT", nullable: true),
                    Calories = table.Column<double>(type: "REAL", nullable: false),
                    ProteinG = table.Column<double>(type: "REAL", nullable: false),
                    CarbsG = table.Column<double>(type: "REAL", nullable: false),
                    FatG = table.Column<double>(type: "REAL", nullable: false),
                    Grams = table.Column<double>(type: "REAL", nullable: false),
                    LoggedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodLogs", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "DietGoals",
                columns: new[] { "Id", "CarbsPercentage", "DailyCalorieLimit", "DeviceId", "FatPercentage", "ProteinPercentage", "UpdatedAt" },
                values: new object[] { 1, 40.0, 2000.0, "default", 30.0, 30.0, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "IX_FoodCache_FdcId",
                table: "FoodCache",
                column: "FdcId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FoodLogs_DeviceId_LoggedAt",
                table: "FoodLogs",
                columns: new[] { "DeviceId", "LoggedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DietGoals");

            migrationBuilder.DropTable(
                name: "FoodCache");

            migrationBuilder.DropTable(
                name: "FoodLogs");
        }
    }
}
