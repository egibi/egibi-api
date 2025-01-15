using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace egibi_api.Migrations
{
    /// <inheritdoc />
    public partial class addadditionalproperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApiKey",
                table: "Connection",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BaseUrl",
                table: "Connection",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecretKey",
                table: "Connection",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Connection",
                keyColumn: "ConnectionID",
                keyValue: 1,
                columns: new[] { "ApiKey", "BaseUrl", "SecretKey" },
                values: new object[] { null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiKey",
                table: "Connection");

            migrationBuilder.DropColumn(
                name: "BaseUrl",
                table: "Connection");

            migrationBuilder.DropColumn(
                name: "SecretKey",
                table: "Connection");
        }
    }
}
