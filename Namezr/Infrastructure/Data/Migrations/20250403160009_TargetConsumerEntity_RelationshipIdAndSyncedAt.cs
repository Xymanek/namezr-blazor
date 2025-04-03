using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace Namezr.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class TargetConsumerEntity_RelationshipIdAndSyncedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastAllConsumersSyncAt",
                table: "SupportTargets");

            migrationBuilder.DropColumn(
                name: "LastSyncedAt",
                table: "ConsumerSupportStatus");

            migrationBuilder.AddColumn<Instant>(
                name: "LastSyncedAt",
                table: "TargetConsumers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelationshipId",
                table: "TargetConsumers",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            // migrationBuilder.DropColumn(
            //     name: "ServiceId",
            //     table: "TargetConsumers");
            
            migrationBuilder.RenameColumn(
                table: "TargetConsumers",
                name: "ServiceId",
                newName: "ServiceUserId"
            );

            migrationBuilder.AlterColumn<string>(
                name: "ServiceUserId",
                table: "TargetConsumers",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException();

            // migrationBuilder.DropColumn(
            //     name: "LastSyncedAt",
            //     table: "TargetConsumers");
            //
            // migrationBuilder.DropColumn(
            //     name: "RelationshipId",
            //     table: "TargetConsumers");
            //
            // migrationBuilder.DropColumn(
            //     name: "ServiceUserId",
            //     table: "TargetConsumers");
            //
            // migrationBuilder.AddColumn<string>(
            //     name: "ServiceId",
            //     table: "TargetConsumers",
            //     type: "text",
            //     nullable: false,
            //     defaultValue: "");
            //
            // migrationBuilder.AddColumn<Instant>(
            //     name: "LastAllConsumersSyncAt",
            //     table: "SupportTargets",
            //     type: "timestamp with time zone",
            //     nullable: true);
            //
            // migrationBuilder.AddColumn<Instant>(
            //     name: "LastSyncedAt",
            //     table: "ConsumerSupportStatus",
            //     type: "timestamp with time zone",
            //     nullable: false,
            //     defaultValue: NodaTime.Instant.FromUnixTimeTicks(0L));
        }
    }
}
