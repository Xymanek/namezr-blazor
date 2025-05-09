using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Namezr.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SubmissionHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubmissionHistoryEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccuredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    InstigatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    InstigatorIsStaff = table.Column<bool>(type: "boolean", nullable: false),
                    InstigatorIsProgrammatic = table.Column<bool>(type: "boolean", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FieldId = table.Column<Guid>(type: "uuid", nullable: true),
                    Comment = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    LabelId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionHistoryEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionHistoryEntries_AspNetUsers_InstigatorUserId",
                        column: x => x.InstigatorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SubmissionHistoryEntries_QuestionnaireFields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "QuestionnaireFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubmissionHistoryEntries_QuestionnaireSubmissions_Submissio~",
                        column: x => x.SubmissionId,
                        principalTable: "QuestionnaireSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubmissionHistoryEntries_SubmissionLabels_LabelId",
                        column: x => x.LabelId,
                        principalTable: "SubmissionLabels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionHistoryEntries_FieldId",
                table: "SubmissionHistoryEntries",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionHistoryEntries_InstigatorUserId",
                table: "SubmissionHistoryEntries",
                column: "InstigatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionHistoryEntries_LabelId",
                table: "SubmissionHistoryEntries",
                column: "LabelId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionHistoryEntries_SubmissionId",
                table: "SubmissionHistoryEntries",
                column: "SubmissionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubmissionHistoryEntries");
        }
    }
}
