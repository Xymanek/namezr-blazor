using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Namezr.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SubmissionNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QuestionnaireSubmissions_VersionId",
                table: "QuestionnaireSubmissions");

            migrationBuilder.CreateTable(
                name: "SubmissionNumberSequences",
                columns: table => new
                {
                    QuestionnaireId = table.Column<Guid>(type: "uuid", nullable: false),
                    Counter = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionNumberSequences", x => x.QuestionnaireId);
                    table.ForeignKey(
                        name: "FK_SubmissionNumberSequences_Questionnaires_QuestionnaireId",
                        column: x => x.QuestionnaireId,
                        principalTable: "Questionnaires",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
            
            migrationBuilder.Sql(
                // language=sql
                """
                CREATE OR REPLACE FUNCTION questionnaire_submission_get_next_number_by_version_id(versionId uuid)
                    RETURNS integer VOLATILE AS
                $$
                DECLARE
                    questionnaire_id uuid;
                    next_counter integer;
                
                BEGIN
                    questionnaire_id := (
                        SELECT QV."QuestionnaireId"
                        FROM "QuestionnaireVersions" QV
                        WHERE QV."Id" = versionId
                    );
                
                    INSERT INTO "SubmissionNumberSequences" ("QuestionnaireId")
                    VALUES (questionnaire_id)
                    ON CONFLICT ("QuestionnaireId") DO UPDATE
                        SET "Counter" = "SubmissionNumberSequences"."Counter" + 1
                    RETURNING "Counter" INTO next_counter;
                
                    RETURN next_counter;
                END
                $$ LANGUAGE plpgsql
                """
            );

            migrationBuilder.AddColumn<int>(
                name: "Number",
                table: "QuestionnaireSubmissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                // language=sql
                """
                CREATE OR REPLACE FUNCTION questionnaire_submission_set_number()
                    RETURNS TRIGGER AS $$
                BEGIN
                    NEW."Number" := questionnaire_submission_get_next_number_by_version_id(NEW."VersionId");
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER set_number
                    BEFORE INSERT ON "QuestionnaireSubmissions"
                    FOR EACH ROW
                EXECUTE FUNCTION questionnaire_submission_set_number();
                """
            );
            
            // Populate existing rows
            migrationBuilder.Sql(
                // language=sql
                """
                DO
                $$
                    DECLARE
                        submission_cursor CURSOR FOR
                            SELECT "Id", "VersionId"
                            FROM "QuestionnaireSubmissions"
                            WHERE "Number" = 0
                            ORDER BY "SubmittedAt";
                        submission_record RECORD;
                    BEGIN
                        OPEN submission_cursor;

                        LOOP
                            FETCH submission_cursor INTO submission_record;
                            EXIT WHEN NOT FOUND;

                            -- Update the submission with the new number
                            UPDATE "QuestionnaireSubmissions"
                            SET "Number" = questionnaire_submission_get_next_number_by_version_id(submission_record."VersionId")
                            WHERE "Id" = submission_record."Id";
                        END LOOP;

                        CLOSE submission_cursor;
                    END
                $$;
                """
                );

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireSubmissions_VersionId_Number",
                table: "QuestionnaireSubmissions",
                columns: new[] { "VersionId", "Number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Number_AboveZero",
                table: "QuestionnaireSubmissions",
                sql: "\"Number\" > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                // language=sql
                """
                DROP TRIGGER set_number ON "QuestionnaireSubmissions";
                DROP FUNCTION IF EXISTS questionnaire_submission_set_number();
                DROP FUNCTION IF EXISTS questionnaire_submission_get_next_number_by_version_id();
                """
            );

            migrationBuilder.DropTable(
                name: "SubmissionNumberSequences");

            migrationBuilder.DropIndex(
                name: "IX_QuestionnaireSubmissions_VersionId_Number",
                table: "QuestionnaireSubmissions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Number_AboveZero",
                table: "QuestionnaireSubmissions");

            migrationBuilder.DropColumn(
                name: "Number",
                table: "QuestionnaireSubmissions");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireSubmissions_VersionId",
                table: "QuestionnaireSubmissions",
                column: "VersionId");
        }
    }
}
