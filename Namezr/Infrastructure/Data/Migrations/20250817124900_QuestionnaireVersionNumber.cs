using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Namezr.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class QuestionnaireVersionNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuestionnaireVersionNumberSequences",
                columns: table => new
                {
                    QuestionnaireId = table.Column<Guid>(type: "uuid", nullable: false),
                    Counter = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionnaireVersionNumberSequences", x => x.QuestionnaireId);
                    table.ForeignKey(
                        name: "FK_QuestionnaireVersionNumberSequences_Questionnaires_QuestionnaireId",
                        column: x => x.QuestionnaireId,
                        principalTable: "Questionnaires",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
            
            migrationBuilder.Sql(
                // language=sql
                """
                CREATE OR REPLACE FUNCTION questionnaire_version_get_next_number_by_questionnaire_id(questionnaireId uuid)
                    RETURNS integer VOLATILE AS
                $$
                DECLARE
                    next_counter integer;
                
                BEGIN
                    INSERT INTO "QuestionnaireVersionNumberSequences" ("QuestionnaireId")
                    VALUES (questionnaireId)
                    ON CONFLICT ("QuestionnaireId") DO UPDATE
                        SET "Counter" = "QuestionnaireVersionNumberSequences"."Counter" + 1
                    RETURNING "Counter" INTO next_counter;
                
                    RETURN next_counter;
                END
                $$ LANGUAGE plpgsql
                """
            );

            migrationBuilder.AddColumn<int>(
                name: "Number",
                table: "QuestionnaireVersions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                // language=sql
                """
                CREATE OR REPLACE FUNCTION questionnaire_version_set_number()
                    RETURNS TRIGGER AS $$
                BEGIN
                    NEW."Number" := questionnaire_version_get_next_number_by_questionnaire_id(NEW."QuestionnaireId");
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER set_version_number
                    BEFORE INSERT ON "QuestionnaireVersions"
                    FOR EACH ROW
                EXECUTE FUNCTION questionnaire_version_set_number();
                """
            );
            
            // Populate existing rows
            migrationBuilder.Sql(
                // language=sql
                """
                DO
                $$
                    DECLARE
                        version_cursor CURSOR FOR
                            SELECT "Id", "QuestionnaireId"
                            FROM "QuestionnaireVersions"
                            WHERE "Number" = 0
                            ORDER BY "CreatedAt";
                        version_record RECORD;
                    BEGIN
                        OPEN version_cursor;

                        LOOP
                            FETCH version_cursor INTO version_record;
                            EXIT WHEN NOT FOUND;

                            -- Update the version with the new number
                            UPDATE "QuestionnaireVersions"
                            SET "Number" = questionnaire_version_get_next_number_by_questionnaire_id(version_record."QuestionnaireId")
                            WHERE "Id" = version_record."Id";
                        END LOOP;

                        CLOSE version_cursor;
                    END
                $$;
                """
                );

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireVersions_QuestionnaireId_Number",
                table: "QuestionnaireVersions",
                columns: new[] { "QuestionnaireId", "Number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Version_Number_AboveZero",
                table: "QuestionnaireVersions",
                sql: "\"Number\" > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                // language=sql
                """
                DROP TRIGGER set_version_number ON "QuestionnaireVersions";
                DROP FUNCTION IF EXISTS questionnaire_version_set_number();
                DROP FUNCTION IF EXISTS questionnaire_version_get_next_number_by_questionnaire_id();
                """
            );

            migrationBuilder.DropTable(
                name: "QuestionnaireVersionNumberSequences");

            migrationBuilder.DropIndex(
                name: "IX_QuestionnaireVersions_QuestionnaireId_Number",
                table: "QuestionnaireVersions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Version_Number_AboveZero",
                table: "QuestionnaireVersions");

            migrationBuilder.DropColumn(
                name: "Number",
                table: "QuestionnaireVersions");
        }
    }
}