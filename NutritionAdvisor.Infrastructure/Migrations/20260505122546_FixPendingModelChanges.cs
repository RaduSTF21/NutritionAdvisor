using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutritionAdvisor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MealPlanItems_Recipes_RecipeId",
                table: "MealPlanItems");

            migrationBuilder.AlterColumn<Guid>(
                name: "RecipeId",
                table: "MealPlanItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<float>(
                name: "Calories",
                table: "MealPlanItems",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Carbs",
                table: "MealPlanItems",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<string>(
                name: "ExternalTitle",
                table: "MealPlanItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalUrl",
                table: "MealPlanItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Fats",
                table: "MealPlanItems",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Protein",
                table: "MealPlanItems",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddForeignKey(
                name: "FK_MealPlanItems_Recipes_RecipeId",
                table: "MealPlanItems",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MealPlanItems_Recipes_RecipeId",
                table: "MealPlanItems");

            migrationBuilder.DropColumn(
                name: "Calories",
                table: "MealPlanItems");

            migrationBuilder.DropColumn(
                name: "Carbs",
                table: "MealPlanItems");

            migrationBuilder.DropColumn(
                name: "ExternalTitle",
                table: "MealPlanItems");

            migrationBuilder.DropColumn(
                name: "ExternalUrl",
                table: "MealPlanItems");

            migrationBuilder.DropColumn(
                name: "Fats",
                table: "MealPlanItems");

            migrationBuilder.DropColumn(
                name: "Protein",
                table: "MealPlanItems");

            migrationBuilder.AlterColumn<Guid>(
                name: "RecipeId",
                table: "MealPlanItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MealPlanItems_Recipes_RecipeId",
                table: "MealPlanItems",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
