using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutritionAdvisor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFoodPreferencesAndAllergies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Allergies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllergenName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SeverityLevel = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Allergies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FoodPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DietType = table.Column<string>(type: "text", nullable: false),
                    PreferredCuisines = table.Column<string>(type: "text", nullable: false),
                    DislikedIngredients = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodPreferences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Allergies_UserId_AllergenName",
                table: "Allergies",
                columns: new[] { "UserId", "AllergenName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Allergies");

            migrationBuilder.DropTable(
                name: "FoodPreferences");
        }
    }
}
