using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace egibi_api.Migrations
{
    /// <inheritdoc />
    public partial class adddescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SecretKey",
                table: "Connection",
                newName: "Description");

            migrationBuilder.AddColumn<string>(
                name: "ApiSecretKey",
                table: "Connection",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Connection",
                keyColumn: "ConnectionID",
                keyValue: 1,
                column: "ApiSecretKey",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiSecretKey",
                table: "Connection");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Connection",
                newName: "SecretKey");
        }
    }
}
