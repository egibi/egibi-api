using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace egibi_api.Migrations
{
    /// <inheritdoc />
    public partial class AddExchangeAccountUserScoping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AppUserId",
                table: "ExchangeAccount",
                type: "integer",
                nullable: true);

            migrationBuilder.InsertData(
                table: "AccountType",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "LastModifiedAt", "Name", "Notes" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 2, 7, 0, 0, 0, 0, DateTimeKind.Utc), "Cryptocurrency exchange account", true, null, "Crypto Exchange", null },
                    { 2, new DateTime(2026, 2, 7, 0, 0, 0, 0, DateTimeKind.Utc), "Stock brokerage account", true, null, "Stock Broker", null },
                    { 3, new DateTime(2026, 2, 7, 0, 0, 0, 0, DateTimeKind.Utc), "Market data provider account", true, null, "Data Provider", null },
                    { 4, new DateTime(2026, 2, 7, 0, 0, 0, 0, DateTimeKind.Utc), "Banking and funding provider account", true, null, "Funding Provider", null },
                    { 5, new DateTime(2026, 2, 7, 0, 0, 0, 0, DateTimeKind.Utc), "Custom account configuration", true, null, "Custom", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeAccount_AppUserId",
                table: "ExchangeAccount",
                column: "AppUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExchangeAccount_AppUser_AppUserId",
                table: "ExchangeAccount",
                column: "AppUserId",
                principalTable: "AppUser",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExchangeAccount_AppUser_AppUserId",
                table: "ExchangeAccount");

            migrationBuilder.DropIndex(
                name: "IX_ExchangeAccount_AppUserId",
                table: "ExchangeAccount");

            migrationBuilder.DeleteData(
                table: "AccountType",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "AccountType",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "AccountType",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "AccountType",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "AccountType",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DropColumn(
                name: "AppUserId",
                table: "ExchangeAccount");
        }
    }
}
