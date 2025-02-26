using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace egibi_api.Migrations
{
    /// <inheritdoc />
    public partial class addunknownconnectiontype : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ConnectionType",
                keyColumn: "ConnectionTypeID",
                keyValue: 1,
                columns: new[] { "Description", "Name" },
                values: new object[] { "unknown connection type", "unknown" });

            migrationBuilder.InsertData(
                table: "ConnectionType",
                columns: new[] { "ConnectionTypeID", "Description", "Name" },
                values: new object[] { 2, "Connection properties for a 3rd party API", "api" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ConnectionType",
                keyColumn: "ConnectionTypeID",
                keyValue: 2);

            migrationBuilder.UpdateData(
                table: "ConnectionType",
                keyColumn: "ConnectionTypeID",
                keyValue: 1,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Connection properties for a 3rd party API", "api" });
        }
    }
}
