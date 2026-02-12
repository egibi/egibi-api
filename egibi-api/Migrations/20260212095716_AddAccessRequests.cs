using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace egibi_api.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InviteCode");

            migrationBuilder.DropTable(
                name: "InviteRequest");

            migrationBuilder.DropColumn(
                name: "Message",
                table: "AccessRequest");

            migrationBuilder.DropColumn(
                name: "ReviewedBy",
                table: "AccessRequest");

            migrationBuilder.RenameColumn(
                name: "RequestedAt",
                table: "AccessRequest",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "RejectionReason",
                table: "AccessRequest",
                newName: "DenialReason");

            migrationBuilder.RenameColumn(
                name: "CreatedUserId",
                table: "AccessRequest",
                newName: "ReviewedByUserId");

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

            migrationBuilder.CreateIndex(
                name: "IX_AccessRequest_Email",
                table: "AccessRequest",
                column: "Email");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AccessRequest_Email",
                table: "AccessRequest");

            migrationBuilder.RenameColumn(
                name: "ReviewedByUserId",
                table: "AccessRequest",
                newName: "CreatedUserId");

            migrationBuilder.RenameColumn(
                name: "DenialReason",
                table: "AccessRequest",
                newName: "RejectionReason");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "AccessRequest",
                newName: "RequestedAt");

            migrationBuilder.AddColumn<string>(
                name: "Message",
                table: "AccessRequest",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedBy",
                table: "AccessRequest",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InviteCode",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UsedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InviteCode", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InviteRequest",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AdminNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    InviteCodeId = table.Column<int>(type: "integer", nullable: true),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InviteRequest", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "AccountType",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 11, 10, 17, 21, 990, DateTimeKind.Utc).AddTicks(6268));

            migrationBuilder.UpdateData(
                table: "AccountType",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 11, 10, 17, 21, 990, DateTimeKind.Utc).AddTicks(6271));

            migrationBuilder.UpdateData(
                table: "AccountType",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 11, 10, 17, 21, 990, DateTimeKind.Utc).AddTicks(6272));

            migrationBuilder.UpdateData(
                table: "AccountType",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 11, 10, 17, 21, 990, DateTimeKind.Utc).AddTicks(6273));

            migrationBuilder.CreateIndex(
                name: "IX_InviteCode_Code",
                table: "InviteCode",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InviteRequest_Email",
                table: "InviteRequest",
                column: "Email");
        }
    }
}
