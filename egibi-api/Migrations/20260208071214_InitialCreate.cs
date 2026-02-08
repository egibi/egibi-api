using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace egibi_api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Password",
                table: "ExchangeAccount");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "ExchangeAccount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "ExchangeAccount",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "ExchangeAccount",
                type: "text",
                nullable: true);
        }
    }
}
