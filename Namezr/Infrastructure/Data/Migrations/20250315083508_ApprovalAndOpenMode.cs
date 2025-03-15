using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace Namezr.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ApprovalAndOpenMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Instant>(
                name: "ApprovedAt",
                table: "QuestionnaireSubmissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApproverId",
                table: "QuestionnaireSubmissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubmissionOpenMode",
                table: "Questionnaires",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "QuestionnaireSubmissions");

            migrationBuilder.DropColumn(
                name: "ApproverId",
                table: "QuestionnaireSubmissions");

            migrationBuilder.DropColumn(
                name: "SubmissionOpenMode",
                table: "Questionnaires");
        }
    }
}
