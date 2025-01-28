using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace Namezr.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreatorsAndSupporters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "QuestionnaireSubmissions",
                type: "uuid",
                nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatorId",
                table: "Questionnaires",
                type: "uuid",
                nullable: false);

            migrationBuilder.CreateTable(
                name: "Creators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Creators", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CreatorStaff",
                columns: table => new
                {
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreatorStaff", x => new { x.CreatorId, x.UserId });
                    table.ForeignKey(
                        name: "FK_CreatorStaff_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CreatorStaff_Creators_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Creators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupportTargets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwningStaffMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffEntryCreatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffEntryUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceType = table.Column<int>(type: "integer", nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ServiceTokenId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportTargets_AspNetUsers_OwningStaffMemberId",
                        column: x => x.OwningStaffMemberId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupportTargets_CreatorStaff_StaffEntryCreatorId_StaffEntryU~",
                        columns: x => new { x.StaffEntryCreatorId, x.StaffEntryUserId },
                        principalTable: "CreatorStaff",
                        principalColumns: new[] { "CreatorId", "UserId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupportTargets_Creators_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Creators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupportTargets_ThirdPartyTokens_ServiceTokenId",
                        column: x => x.ServiceTokenId,
                        principalTable: "ThirdPartyTokens",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SupportPlans",
                columns: table => new
                {
                    SupportTargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupportPlanId = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportPlans", x => new { x.SupportTargetId, x.SupportPlanId });
                    table.ForeignKey(
                        name: "FK_SupportPlans_SupportTargets_SupportTargetId",
                        column: x => x.SupportTargetId,
                        principalTable: "SupportTargets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TargetConsumers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupportTargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TargetConsumers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TargetConsumers_SupportTargets_SupportTargetId",
                        column: x => x.SupportTargetId,
                        principalTable: "SupportTargets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsumerSupportStatus",
                columns: table => new
                {
                    ConsumerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupportPlanId = table.Column<string>(type: "text", nullable: false),
                    TargetConsumerId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    EnrolledAt = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<Instant>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumerSupportStatus", x => new { x.ConsumerId, x.SupportPlanId });
                    table.ForeignKey(
                        name: "FK_ConsumerSupportStatus_TargetConsumers_TargetConsumerId",
                        column: x => x.TargetConsumerId,
                        principalTable: "TargetConsumers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireSubmissions_UserId",
                table: "QuestionnaireSubmissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Questionnaires_CreatorId",
                table: "Questionnaires",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumerSupportStatus_TargetConsumerId",
                table: "ConsumerSupportStatus",
                column: "TargetConsumerId");

            migrationBuilder.CreateIndex(
                name: "IX_CreatorStaff_UserId",
                table: "CreatorStaff",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTargets_CreatorId",
                table: "SupportTargets",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTargets_OwningStaffMemberId",
                table: "SupportTargets",
                column: "OwningStaffMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTargets_ServiceTokenId",
                table: "SupportTargets",
                column: "ServiceTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTargets_StaffEntryCreatorId_StaffEntryUserId",
                table: "SupportTargets",
                columns: new[] { "StaffEntryCreatorId", "StaffEntryUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_TargetConsumers_SupportTargetId",
                table: "TargetConsumers",
                column: "SupportTargetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Questionnaires_Creators_CreatorId",
                table: "Questionnaires",
                column: "CreatorId",
                principalTable: "Creators",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QuestionnaireSubmissions_AspNetUsers_UserId",
                table: "QuestionnaireSubmissions",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questionnaires_Creators_CreatorId",
                table: "Questionnaires");

            migrationBuilder.DropForeignKey(
                name: "FK_QuestionnaireSubmissions_AspNetUsers_UserId",
                table: "QuestionnaireSubmissions");

            migrationBuilder.DropTable(
                name: "ConsumerSupportStatus");

            migrationBuilder.DropTable(
                name: "SupportPlans");

            migrationBuilder.DropTable(
                name: "TargetConsumers");

            migrationBuilder.DropTable(
                name: "SupportTargets");

            migrationBuilder.DropTable(
                name: "CreatorStaff");

            migrationBuilder.DropTable(
                name: "Creators");

            migrationBuilder.DropIndex(
                name: "IX_QuestionnaireSubmissions_UserId",
                table: "QuestionnaireSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_Questionnaires_CreatorId",
                table: "Questionnaires");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "QuestionnaireSubmissions");

            migrationBuilder.DropColumn(
                name: "CreatorId",
                table: "Questionnaires");
        }
    }
}
