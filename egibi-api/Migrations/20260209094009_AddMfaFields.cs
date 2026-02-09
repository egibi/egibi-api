using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace egibi_api.Migrations
{
    /// <inheritdoc />
    public partial class AddMfaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EncryptedRecoveryCodes",
                table: "AppUser",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptedTotpSecret",
                table: "AppUser",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsMfaEnabled",
                table: "AppUser",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedRecoveryCodes",
                table: "AppUser");

            migrationBuilder.DropColumn(
                name: "EncryptedTotpSecret",
                table: "AppUser");

            migrationBuilder.DropColumn(
                name: "IsMfaEnabled",
                table: "AppUser");
        }
    }
}
