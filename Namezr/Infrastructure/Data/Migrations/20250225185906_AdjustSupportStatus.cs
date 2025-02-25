using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace Namezr.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdjustSupportStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConsumerSupportStatus_TargetConsumers_TargetConsumerId",
                table: "ConsumerSupportStatus");

            migrationBuilder.DropIndex(
                name: "IX_ConsumerSupportStatus_TargetConsumerId",
                table: "ConsumerSupportStatus");

            migrationBuilder.DropColumn(
                name: "TargetConsumerId",
                table: "ConsumerSupportStatus");

            migrationBuilder.AlterColumn<string>(
                name: "SupportPlanId",
                table: "ConsumerSupportStatus",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Instant>(
                name: "LastSyncedAt",
                table: "ConsumerSupportStatus",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: NodaTime.Instant.FromUnixTimeTicks(0L));

            migrationBuilder.AddForeignKey(
                name: "FK_ConsumerSupportStatus_TargetConsumers_ConsumerId",
                table: "ConsumerSupportStatus",
                column: "ConsumerId",
                principalTable: "TargetConsumers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConsumerSupportStatus_TargetConsumers_ConsumerId",
                table: "ConsumerSupportStatus");

            migrationBuilder.DropColumn(
                name: "LastSyncedAt",
                table: "ConsumerSupportStatus");

            migrationBuilder.AlterColumn<string>(
                name: "SupportPlanId",
                table: "ConsumerSupportStatus",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<Guid>(
                name: "TargetConsumerId",
                table: "ConsumerSupportStatus",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ConsumerSupportStatus_TargetConsumerId",
                table: "ConsumerSupportStatus",
                column: "TargetConsumerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ConsumerSupportStatus_TargetConsumers_TargetConsumerId",
                table: "ConsumerSupportStatus",
                column: "TargetConsumerId",
                principalTable: "TargetConsumers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
