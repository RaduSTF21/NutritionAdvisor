using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutritionAdvisor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAIModifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // SonarCloud Fix: Explicitly state that downgrading this migration is not supported
            // to prevent accidental data loss.
            throw new NotSupportedException("Downgrading this migration is not supported.");
        }
    }
}
