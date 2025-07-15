using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Namezr.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SubmissionAttributeAuditHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "SubmissionHistoryEntries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousValue",
                table: "SubmissionHistoryEntries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Value",
                table: "SubmissionHistoryEntries",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Key",
                table: "SubmissionHistoryEntries");

            migrationBuilder.DropColumn(
                name: "PreviousValue",
                table: "SubmissionHistoryEntries");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "SubmissionHistoryEntries");
        }
    }
}
