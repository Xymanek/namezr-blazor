using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace Namezr.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Questionnaires",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ApprovalMode = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questionnaires", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuestionnaireFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionnaireId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionnaireFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionnaireFields_Questionnaires_QuestionnaireId",
                        column: x => x.QuestionnaireId,
                        principalTable: "Questionnaires",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionnaireVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionnaireId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionnaireVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionnaireVersions_Questionnaires_QuestionnaireId",
                        column: x => x.QuestionnaireId,
                        principalTable: "Questionnaires",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionnaireFieldConfigurations",
                columns: table => new
                {
                    FieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    FileUploadOptions = table.Column<string>(type: "jsonb", nullable: true),
                    NumberOptions = table.Column<string>(type: "jsonb", nullable: true),
                    TextOptions = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionnaireFieldConfigurations", x => new { x.FieldId, x.VersionId });
                    table.ForeignKey(
                        name: "FK_QuestionnaireFieldConfigurations_QuestionnaireFields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "QuestionnaireFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestionnaireFieldConfigurations_QuestionnaireVersions_Vers~",
                        column: x => x.VersionId,
                        principalTable: "QuestionnaireVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionnaireSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionnaireSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionnaireSubmissions_QuestionnaireVersions_VersionId",
                        column: x => x.VersionId,
                        principalTable: "QuestionnaireVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionnaireFieldValues",
                columns: table => new
                {
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValueSerialized = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionnaireFieldValues", x => new { x.SubmissionId, x.FieldId });
                    table.ForeignKey(
                        name: "FK_QuestionnaireFieldValues_QuestionnaireFields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "QuestionnaireFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestionnaireFieldValues_QuestionnaireSubmissions_Submissio~",
                        column: x => x.SubmissionId,
                        principalTable: "QuestionnaireSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireFieldConfigurations_VersionId",
                table: "QuestionnaireFieldConfigurations",
                column: "VersionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireFields_QuestionnaireId",
                table: "QuestionnaireFields",
                column: "QuestionnaireId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireFieldValues_FieldId",
                table: "QuestionnaireFieldValues",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireSubmissions_VersionId",
                table: "QuestionnaireSubmissions",
                column: "VersionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireVersions_QuestionnaireId",
                table: "QuestionnaireVersions",
                column: "QuestionnaireId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuestionnaireFieldConfigurations");

            migrationBuilder.DropTable(
                name: "QuestionnaireFieldValues");

            migrationBuilder.DropTable(
                name: "QuestionnaireFields");

            migrationBuilder.DropTable(
                name: "QuestionnaireSubmissions");

            migrationBuilder.DropTable(
                name: "QuestionnaireVersions");

            migrationBuilder.DropTable(
                name: "Questionnaires");
        }
    }
}
