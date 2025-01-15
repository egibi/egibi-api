using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace egibi_api.Migrations
{
    /// <inheritdoc />
    public partial class initialize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConnectionType",
                columns: table => new
                {
                    ConnectionTypeID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectionType", x => x.ConnectionTypeID);
                });

            migrationBuilder.CreateTable(
                name: "Connection",
                columns: table => new
                {
                    ConnectionID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    ConnectionTypeID = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Connection", x => x.ConnectionID);
                    table.ForeignKey(
                        name: "FK_Connection_ConnectionType_ConnectionTypeID",
                        column: x => x.ConnectionTypeID,
                        principalTable: "ConnectionType",
                        principalColumn: "ConnectionTypeID");
                });

            migrationBuilder.InsertData(
                table: "ConnectionType",
                columns: new[] { "ConnectionTypeID", "Description", "Name" },
                values: new object[] { 1, "Connection properties for a 3rd party API", "api" });

            migrationBuilder.InsertData(
                table: "Connection",
                columns: new[] { "ConnectionID", "ConnectionTypeID", "Name" },
                values: new object[] { 1, 1, "Binance US" });

            migrationBuilder.CreateIndex(
                name: "IX_Connection_ConnectionTypeID",
                table: "Connection",
                column: "ConnectionTypeID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Connection");

            migrationBuilder.DropTable(
                name: "ConnectionType");
        }
    }
}
