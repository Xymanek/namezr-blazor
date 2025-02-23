using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Namezr.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class EligibilityOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "SupportPlans",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SupportPlanId",
                table: "SupportPlans",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<long>(
                name: "EligibilityConfigurationId",
                table: "Questionnaires",
                type: "bigint",
                nullable: false);

            migrationBuilder.CreateTable(
                name: "EligibilityConfigurations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OwnershipType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EligibilityConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EligibilityOptions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConfigurationId = table.Column<long>(type: "bigint", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    PriorityGroup = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PriorityModifier = table.Column<decimal>(type: "numeric", nullable: false),
                    PlanId_Type = table.Column<int>(type: "integer", nullable: false),
                    PlanId_VirtualEligibilityType = table.Column<int>(type: "integer", nullable: false),
                    PlanId_SupportPlanId_SupportPlanId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PlanId_SupportPlanId_SupportTargetId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EligibilityOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EligibilityOptions_EligibilityConfigurations_ConfigurationId",
                        column: x => x.ConfigurationId,
                        principalTable: "EligibilityConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Questionnaires_EligibilityConfigurationId",
                table: "Questionnaires",
                column: "EligibilityConfigurationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EligibilityOptions_ConfigurationId",
                table: "EligibilityOptions",
                column: "ConfigurationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Questionnaires_EligibilityConfigurations_EligibilityConfigu~",
                table: "Questionnaires",
                column: "EligibilityConfigurationId",
                principalTable: "EligibilityConfigurations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questionnaires_EligibilityConfigurations_EligibilityConfigu~",
                table: "Questionnaires");

            migrationBuilder.DropTable(
                name: "EligibilityOptions");

            migrationBuilder.DropTable(
                name: "EligibilityConfigurations");

            migrationBuilder.DropIndex(
                name: "IX_Questionnaires_EligibilityConfigurationId",
                table: "Questionnaires");

            migrationBuilder.DropColumn(
                name: "EligibilityConfigurationId",
                table: "Questionnaires");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "SupportPlans",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SupportPlanId",
                table: "SupportPlans",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);
        }
    }
}
