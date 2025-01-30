using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace egibi_api.Migrations
{
    /// <inheritdoc />
    public partial class fixschwabconnectiontypeid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Connection",
                keyColumn: "ConnectionID",
                keyValue: 3,
                column: "ConnectionTypeID",
                value: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Connection",
                keyColumn: "ConnectionID",
                keyValue: 3,
                column: "ConnectionTypeID",
                value: null);
        }
    }
}
