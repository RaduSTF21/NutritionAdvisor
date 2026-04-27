using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutritionAdvisor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMealsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyLogRecipes");

            migrationBuilder.CreateTable(
                name: "Meals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EatenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LogId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Meals_DailyLogs_LogId",
                        column: x => x.LogId,
                        principalTable: "DailyLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Meals_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Meals_LogId",
                table: "Meals",
                column: "LogId");

            migrationBuilder.CreateIndex(
                name: "IX_Meals_RecipeId",
                table: "Meals",
                column: "RecipeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Meals");

            migrationBuilder.CreateTable(
                name: "DailyLogRecipes",
                columns: table => new
                {
                    ConsumedRecipesId = table.Column<Guid>(type: "uuid", nullable: false),
                    DailyLogId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyLogRecipes", x => new { x.ConsumedRecipesId, x.DailyLogId });
                    table.ForeignKey(
                        name: "FK_DailyLogRecipes_DailyLogs_DailyLogId",
                        column: x => x.DailyLogId,
                        principalTable: "DailyLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DailyLogRecipes_Recipes_ConsumedRecipesId",
                        column: x => x.ConsumedRecipesId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyLogRecipes_DailyLogId",
                table: "DailyLogRecipes",
                column: "DailyLogId");
        }
    }
}
