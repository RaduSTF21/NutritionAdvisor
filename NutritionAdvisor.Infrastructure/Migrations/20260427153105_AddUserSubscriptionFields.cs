using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutritionAdvisor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSubscriptionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoRenew",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProviderCustomerId",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderSubscriptionId",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionEndAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionPlan",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionStartAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionStatus",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "TrialEndsAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ProviderSubscriptionId",
                table: "Users",
                column: "ProviderSubscriptionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_ProviderSubscriptionId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AutoRenew",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProviderCustomerId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProviderSubscriptionId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SubscriptionEndAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SubscriptionPlan",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SubscriptionStartAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SubscriptionStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TrialEndsAt",
                table: "Users");
        }
    }
}
