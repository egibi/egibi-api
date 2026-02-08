using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace egibi_api.Migrations
{
    /// <inheritdoc />
    public partial class AddFundingSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApiDocsUrl",
                table: "Connection",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkMethod",
                table: "Connection",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignupUrl",
                table: "Connection",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FundingSource",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppUserId = table.Column<int>(type: "integer", nullable: false),
                    ConnectionId = table.Column<int>(type: "integer", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    BaseUrl = table.Column<string>(type: "text", nullable: true),
                    LinkMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PlaidItemId = table.Column<string>(type: "text", nullable: true),
                    EncryptedPlaidAccessToken = table.Column<string>(type: "text", nullable: true),
                    PlaidAccountId = table.Column<string>(type: "text", nullable: true),
                    PlaidAccountName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PlaidAccountMask = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundingSource", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FundingSource_AppUser_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FundingSource_Connection_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "Connection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Connection",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApiDocsUrl", "LinkMethod", "SignupUrl" },
                values: new object[] { null, "api_key", null });

            migrationBuilder.UpdateData(
                table: "Connection",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ApiDocsUrl", "LinkMethod", "SignupUrl" },
                values: new object[] { null, "api_key", null });

            migrationBuilder.UpdateData(
                table: "Connection",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ApiDocsUrl", "LinkMethod", "SignupUrl" },
                values: new object[] { null, "api_key", null });

            migrationBuilder.UpdateData(
                table: "Connection",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "ApiDocsUrl", "LinkMethod", "SignupUrl" },
                values: new object[] { null, "api_key", null });

            migrationBuilder.UpdateData(
                table: "Connection",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "ApiDocsUrl", "LinkMethod", "SignupUrl" },
                values: new object[] { null, "api_key", null });

            migrationBuilder.UpdateData(
                table: "Connection",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "ApiDocsUrl", "LinkMethod", "SignupUrl" },
                values: new object[] { null, "api_key", null });

            migrationBuilder.UpdateData(
                table: "Connection",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "ApiDocsUrl", "LinkMethod", "SignupUrl" },
                values: new object[] { null, "api_key", null });

            migrationBuilder.UpdateData(
                table: "Connection",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "ApiDocsUrl", "LinkMethod", "SignupUrl" },
                values: new object[] { null, "api_key", null });

            migrationBuilder.UpdateData(
                table: "Connection",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "ApiDocsUrl", "LinkMethod", "SignupUrl" },
                values: new object[] { null, "api_key", null });

            migrationBuilder.UpdateData(
                table: "Connection",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "ApiDocsUrl", "Color", "Description", "LinkMethod", "SignupUrl" },
                values: new object[] { "https://docs.mercury.com", "#6366F1", "Business banking with powerful API access for programmatic fund management", "api_key", "https://app.mercury.com/signup" });

            migrationBuilder.InsertData(
                table: "Connection",
                columns: new[] { "Id", "ApiDocsUrl", "ApiKey", "ApiSecretKey", "BaseUrl", "Category", "Color", "ConnectionTypeId", "CreatedAt", "DefaultBaseUrl", "Description", "IconKey", "IsActive", "IsDataSource", "LastModifiedAt", "LinkMethod", "Name", "Notes", "RequiredFields", "SignupUrl", "SortOrder", "Website" },
                values: new object[] { 11, "https://plaid.com/docs", null, null, null, "funding_provider", "#00D09C", 2, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "https://production.plaid.com", "Connect any US bank account securely via Plaid Link", "plaid", true, false, null, "plaid_link", "Plaid", null, "[]", "", 31, "https://plaid.com" });

            migrationBuilder.CreateIndex(
                name: "IX_FundingSource_AppUserId_PrimaryUnique",
                table: "FundingSource",
                column: "AppUserId",
                unique: true,
                filter: "\"IsPrimary\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_FundingSource_ConnectionId",
                table: "FundingSource",
                column: "ConnectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FundingSource");

            migrationBuilder.DeleteData(
                table: "Connection",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DropColumn(
                name: "ApiDocsUrl",
                table: "Connection");

            migrationBuilder.DropColumn(
                name: "LinkMethod",
                table: "Connection");

            migrationBuilder.DropColumn(
                name: "SignupUrl",
                table: "Connection");

            migrationBuilder.UpdateData(
                table: "Connection",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Color", "Description" },
                values: new object[] { "#5C5CFF", "API-first banking for startups and businesses with free ACH transfers" });
        }
    }
}
