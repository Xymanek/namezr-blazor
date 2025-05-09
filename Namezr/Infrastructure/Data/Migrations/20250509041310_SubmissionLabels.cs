using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Namezr.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SubmissionLabels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubmissionLabels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Colour = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    IsSubmitterVisible = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionLabels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionLabels_Creators_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Creators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionnaireSubmissions_Labels",
                columns: table => new
                {
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    LabelId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionnaireSubmissions_Labels", x => new { x.LabelId, x.SubmissionId });
                    table.ForeignKey(
                        name: "FK_QuestionnaireSubmissions_Labels_QuestionnaireSubmissions_Su~",
                        column: x => x.SubmissionId,
                        principalTable: "QuestionnaireSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestionnaireSubmissions_Labels_SubmissionLabels_LabelId",
                        column: x => x.LabelId,
                        principalTable: "SubmissionLabels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireSubmissions_Labels_SubmissionId",
                table: "QuestionnaireSubmissions_Labels",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionLabels_CreatorId_Text",
                table: "SubmissionLabels",
                columns: new[] { "CreatorId", "Text" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuestionnaireSubmissions_Labels");

            migrationBuilder.DropTable(
                name: "SubmissionLabels");
        }
    }
}
