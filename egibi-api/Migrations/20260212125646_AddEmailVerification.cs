using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace egibi_api.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "AccessRequest",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationExpiresAt",
                table: "AccessRequest",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailVerificationToken",
                table: "AccessRequest",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerifiedAt",
                table: "AccessRequest",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AccountType",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 12, 12, 56, 45, 724, DateTimeKind.Utc).AddTicks(172));

            migrationBuilder.UpdateData(
                table: "AccountType",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 12, 12, 56, 45, 724, DateTimeKind.Utc).AddTicks(177));

            migrationBuilder.UpdateData(
                table: "AccountType",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 12, 12, 56, 45, 724, DateTimeKind.Utc).AddTicks(178));

            migrationBuilder.UpdateData(
                table: "AccountType",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 12, 12, 56, 45, 724, DateTimeKind.Utc).AddTicks(179));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailVerificationExpiresAt",
                table: "AccessRequest");

            migrationBuilder.DropColumn(
                name: "EmailVerificationToken",
                table: "AccessRequest");

            migrationBuilder.DropColumn(
                name: "EmailVerifiedAt",
                table: "AccessRequest");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "AccessRequest",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.UpdateData(
                table: "AccountType",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 12, 9, 57, 15, 981, DateTimeKind.Utc).AddTicks(7399));

            migrationBuilder.UpdateData(
                table: "AccountType",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 12, 9, 57, 15, 981, DateTimeKind.Utc).AddTicks(7402));

            migrationBuilder.UpdateData(
                table: "AccountType",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 12, 9, 57, 15, 981, DateTimeKind.Utc).AddTicks(7403));

            migrationBuilder.UpdateData(
                table: "AccountType",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 12, 9, 57, 15, 981, DateTimeKind.Utc).AddTicks(7404));
        }
    }
}
