using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Namezr.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SupportTargetDisplayInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "SupportTargets",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HomeUrl",
                table: "SupportTargets",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JoinUrl",
                table: "SupportTargets",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LogoFileId",
                table: "SupportTargets",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "SupportTargets");

            migrationBuilder.DropColumn(
                name: "HomeUrl",
                table: "SupportTargets");

            migrationBuilder.DropColumn(
                name: "JoinUrl",
                table: "SupportTargets");

            migrationBuilder.DropColumn(
                name: "LogoFileId",
                table: "SupportTargets");
        }
    }
}
