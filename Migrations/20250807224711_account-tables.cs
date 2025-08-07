using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace egibi_api.Migrations
{
    /// <inheritdoc />
    public partial class accounttables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Password",
                table: "Account");

            migrationBuilder.RenameColumn(
                name: "Username",
                table: "Account",
                newName: "Url");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Url",
                table: "Account",
                newName: "Username");

            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "Account",
                type: "text",
                nullable: true);
        }
    }
}
