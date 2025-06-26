using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Namezr.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMaxSubmissionsToEligibilityOption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxSubmissionsPerUser",
                table: "EligibilityOptions",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxSubmissionsPerUser",
                table: "EligibilityOptions");
        }
    }
}
