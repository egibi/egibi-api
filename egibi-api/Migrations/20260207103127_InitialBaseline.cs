using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace egibi_api.Migrations
{
    /// <inheritdoc />
    public partial class InitialBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPrimaryFunding",
                table: "Account",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.InsertData(
                table: "Connection",
                columns: new[] { "Id", "ApiKey", "ApiSecretKey", "BaseUrl", "Category", "Color", "ConnectionTypeId", "CreatedAt", "DefaultBaseUrl", "Description", "IconKey", "IsActive", "IsDataSource", "LastModifiedAt", "Name", "Notes", "RequiredFields", "SortOrder", "Website" },
                values: new object[] { 10, null, null, null, "funding_provider", "#5C5CFF", 2, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "https://api.mercury.com/api/v1", "API-first banking for startups and businesses with free ACH transfers", "mercury", true, false, null, "Mercury", null, "[\"api_key\"]", 30, "https://mercury.com" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Connection",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DropColumn(
                name: "IsPrimaryFunding",
                table: "Account");
        }
    }
}
