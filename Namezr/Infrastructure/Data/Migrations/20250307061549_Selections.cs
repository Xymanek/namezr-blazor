using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Namezr.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Selections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SelectionSeries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OwnershipType = table.Column<int>(type: "integer", nullable: false),
                    QuestionnaireId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompleteCyclesCount = table.Column<int>(type: "integer", nullable: false),
                    CompletedSelectionMarker = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelectionSeries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SelectionSeries_Questionnaires_QuestionnaireId",
                        column: x => x.QuestionnaireId,
                        principalTable: "Questionnaires",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SelectionBatches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SeriesId = table.Column<Guid>(type: "uuid", nullable: false),
                    RollStartedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    RollCompletedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelectionBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SelectionBatches_SelectionSeries_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "SelectionSeries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SelectionUserData",
                columns: table => new
                {
                    SeriesId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LatestModifier = table.Column<decimal>(type: "numeric", nullable: false),
                    SelectedCount = table.Column<int>(type: "integer", nullable: false),
                    TotalSelectedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelectionUserData", x => new { x.SeriesId, x.UserId });
                    table.ForeignKey(
                        name: "FK_SelectionUserData_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SelectionUserData_SelectionSeries_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "SelectionSeries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SelectionEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BatchId = table.Column<long>(type: "bigint", nullable: false),
                    BatchPosition = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: true),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: true),
                    Cycle = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelectionEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SelectionEntries_SelectionBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "SelectionBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SelectionBatches_SeriesId",
                table: "SelectionBatches",
                column: "SeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_SelectionEntries_BatchId",
                table: "SelectionEntries",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_SelectionEntries_CandidateId",
                table: "SelectionEntries",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_SelectionSeries_QuestionnaireId",
                table: "SelectionSeries",
                column: "QuestionnaireId");

            migrationBuilder.CreateIndex(
                name: "IX_SelectionUserData_UserId",
                table: "SelectionUserData",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SelectionEntries");

            migrationBuilder.DropTable(
                name: "SelectionUserData");

            migrationBuilder.DropTable(
                name: "SelectionBatches");

            migrationBuilder.DropTable(
                name: "SelectionSeries");
        }
    }
}
