using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Namezr.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class EligibilityOptions2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlanId_SupportPlanId_SupportPlanId",
                table: "EligibilityOptions");

            migrationBuilder.DropColumn(
                name: "PlanId_SupportPlanId_SupportTargetId",
                table: "EligibilityOptions");

            migrationBuilder.DropColumn(
                name: "PlanId_Type",
                table: "EligibilityOptions");

            migrationBuilder.DropColumn(
                name: "PlanId_VirtualEligibilityType",
                table: "EligibilityOptions");

            migrationBuilder.AddColumn<string>(
                name: "PlanId",
                table: "EligibilityOptions",
                type: "jsonb",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlanId",
                table: "EligibilityOptions");

            migrationBuilder.AddColumn<string>(
                name: "PlanId_SupportPlanId_SupportPlanId",
                table: "EligibilityOptions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "PlanId_SupportPlanId_SupportTargetId",
                table: "EligibilityOptions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "PlanId_Type",
                table: "EligibilityOptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PlanId_VirtualEligibilityType",
                table: "EligibilityOptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
