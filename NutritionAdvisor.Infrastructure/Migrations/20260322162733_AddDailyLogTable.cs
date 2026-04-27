using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutritionAdvisor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyLogTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyLogs", x => x.Id);
                });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyLogRecipes");

            migrationBuilder.DropTable(
                name: "DailyLogs");
        }
    }
}
