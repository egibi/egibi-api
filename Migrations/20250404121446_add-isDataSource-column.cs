using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace egibi_api.Migrations
{
    /// <inheritdoc />
    public partial class addisDataSourcecolumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDataSource",
                table: "Connection",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Connection",
                keyColumn: "ConnectionID",
                keyValue: 1,
                column: "IsDataSource",
                value: true);

            migrationBuilder.UpdateData(
                table: "Connection",
                keyColumn: "ConnectionID",
                keyValue: 2,
                column: "IsDataSource",
                value: true);

            migrationBuilder.UpdateData(
                table: "Connection",
                keyColumn: "ConnectionID",
                keyValue: 3,
                column: "IsDataSource",
                value: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDataSource",
                table: "Connection");
        }
    }
}
