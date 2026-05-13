using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutritionAdvisor.Infrastructure.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class FixPendingModelChanges : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "UserProfiles",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValue: Guid.Empty);

            migrationBuilder.AlterColumn<List<string>>(
                name: "PreferredCuisines",
                table: "FoodPreferences",
                type: "text[]",
                nullable: false,
                oldClrType: typeof(List<string>),
                oldType: "text[]",
                oldDefaultValueSql: "ARRAY[]::text[]");

            migrationBuilder.AlterColumn<List<string>>(
                name: "DislikedIngredients",
                table: "FoodPreferences",
                type: "text[]",
                nullable: false,
                oldClrType: typeof(List<string>),
                oldType: "text[]",
                oldDefaultValueSql: "ARRAY[]::text[]");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Downgrading this migration is not supported.");
        }
    }
}