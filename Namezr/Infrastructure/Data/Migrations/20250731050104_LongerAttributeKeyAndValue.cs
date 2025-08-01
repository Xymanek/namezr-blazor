using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Namezr.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class LongerAttributeKeyAndValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "SubmissionAttributes",
                type: "character varying(15000)",
                maxLength: 15000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(5000)",
                oldMaxLength: 5000);

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                table: "SubmissionAttributes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "SubmissionAttributes",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(15000)",
                oldMaxLength: 15000);

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                table: "SubmissionAttributes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);
        }
    }
}
