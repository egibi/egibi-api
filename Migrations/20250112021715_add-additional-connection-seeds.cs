using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace egibi_api.Migrations
{
    /// <inheritdoc />
    public partial class addadditionalconnectionseeds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Connection",
                columns: new[] { "ConnectionID", "ApiKey", "ApiSecretKey", "BaseUrl", "ConnectionTypeID", "Description", "Name" },
                values: new object[,]
                {
                    { 2, null, null, null, 1, null, "Coinbase" },
                    { 3, null, null, null, null, null, "Charles Schwab" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Connection",
                keyColumn: "ConnectionID",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Connection",
                keyColumn: "ConnectionID",
                keyValue: 3);
        }
    }
}
