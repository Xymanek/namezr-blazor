using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Namezr.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedThirdPartyTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ThirdPartyTokenId",
                table: "AspNetUserLogins",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ThirdPartyTokens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServiceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ServiceAccountId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TokenType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<byte[]>(type: "bytea", nullable: false),
                    Context = table.Column<JsonDocument>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThirdPartyTokens", x => x.Id);
                    table.UniqueConstraint("AK_ThirdPartyTokens_ServiceType_ServiceAccountId_TokenType", x => new { x.ServiceType, x.ServiceAccountId, x.TokenType });
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_ThirdPartyTokenId",
                table: "AspNetUserLogins",
                column: "ThirdPartyTokenId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThirdPartyTokens_ServiceType_ServiceAccountId",
                table: "ThirdPartyTokens",
                columns: new[] { "ServiceType", "ServiceAccountId" });

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserLogins_ThirdPartyTokens_ThirdPartyTokenId",
                table: "AspNetUserLogins",
                column: "ThirdPartyTokenId",
                principalTable: "ThirdPartyTokens",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserLogins_ThirdPartyTokens_ThirdPartyTokenId",
                table: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "ThirdPartyTokens");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUserLogins_ThirdPartyTokenId",
                table: "AspNetUserLogins");

            migrationBuilder.DropColumn(
                name: "ThirdPartyTokenId",
                table: "AspNetUserLogins");
        }
    }
}
