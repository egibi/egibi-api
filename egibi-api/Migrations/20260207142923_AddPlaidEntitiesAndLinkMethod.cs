using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace egibi_api.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaidEntitiesAndLinkMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LinkMethod",
                table: "Connection",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlaidItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppUserId = table.Column<int>(type: "integer", nullable: false),
                    AccountId = table.Column<int>(type: "integer", nullable: true),
                    PlaidItemId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EncryptedAccessToken = table.Column<string>(type: "text", nullable: false),
                    InstitutionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    InstitutionName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EnabledProducts = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ConsentExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaidItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaidItem_Account_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PlaidItem_AppUser_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlaidAccount",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlaidItemId = table.Column<int>(type: "integer", nullable: false),
                    PlaidAccountId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OfficialName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Mask = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    AccountType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AccountSubtype = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsSelectedFunding = table.Column<bool>(type: "boolean", nullable: false),
                    AvailableBalance = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    CurrentBalance = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    IsoCurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    BalanceLastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaidAccount", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaidAccount_PlaidItem_PlaidItemId",
                        column: x => x.PlaidItemId,
                        principalTable: "PlaidItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Connection",
                keyColumn: "Id",
                keyValue: 1,
                column: "LinkMethod",
                value: "api_key");

            migrationBuilder.UpdateData(
                table: "Connection",
                keyColumn: "Id",
                keyValue: 2,
                column: "LinkMethod",
                value: "api_key");

            migrationBuilder.UpdateData(
                table: "Connection",
                keyColumn: "Id",
                keyValue: 3,
                column: "LinkMethod",
                value: "api_key");

            migrationBuilder.UpdateData(
                table: "Connection",
                keyColumn: "Id",
                keyValue: 4,
                column: "LinkMethod",
                value: "api_key");

            migrationBuilder.UpdateData(
                table: "Connection",
                keyColumn: "Id",
                keyValue: 5,
                column: "LinkMethod",
                value: "api_key");

            migrationBuilder.UpdateData(
                table: "Connection",
                keyColumn: "Id",
                keyValue: 6,
                column: "LinkMethod",
                value: "api_key");

            migrationBuilder.UpdateData(
                table: "Connection",
                keyColumn: "Id",
                keyValue: 7,
                column: "LinkMethod",
                value: "api_key");

            migrationBuilder.UpdateData(
                table: "Connection",
                keyColumn: "Id",
                keyValue: 8,
                column: "LinkMethod",
                value: "api_key");

            migrationBuilder.UpdateData(
                table: "Connection",
                keyColumn: "Id",
                keyValue: 9,
                column: "LinkMethod",
                value: "api_key");

            migrationBuilder.UpdateData(
                table: "Connection",
                keyColumn: "Id",
                keyValue: 10,
                column: "LinkMethod",
                value: "api_key");

            migrationBuilder.InsertData(
                table: "Connection",
                columns: new[] { "Id", "ApiKey", "ApiSecretKey", "BaseUrl", "Category", "Color", "ConnectionTypeId", "CreatedAt", "DefaultBaseUrl", "Description", "IconKey", "IsActive", "IsDataSource", "LastModifiedAt", "LinkMethod", "Name", "Notes", "RequiredFields", "SortOrder", "Website" },
                values: new object[] { 11, null, null, null, "funding_provider", "#00D09C", 2, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "https://development.plaid.com", "Link any US bank account for balance tracking, transactions, and ACH transfers", "plaid", true, false, null, "plaid_link", "Plaid", null, "[]", 31, "https://plaid.com" });

            migrationBuilder.CreateIndex(
                name: "IX_PlaidAccount_PlaidItemId_PlaidAccountId",
                table: "PlaidAccount",
                columns: new[] { "PlaidItemId", "PlaidAccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlaidItem_AccountId",
                table: "PlaidItem",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaidItem_AppUserId_PlaidItemId",
                table: "PlaidItem",
                columns: new[] { "AppUserId", "PlaidItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlaidAccount");

            migrationBuilder.DropTable(
                name: "PlaidItem");

            migrationBuilder.DeleteData(
                table: "Connection",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DropColumn(
                name: "LinkMethod",
                table: "Connection");
        }
    }
}
