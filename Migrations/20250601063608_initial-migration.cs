using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace egibi_api.Migrations
{
    /// <inheritdoc />
    public partial class initialmigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConnectionType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectionType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataFormatType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataFormatType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataFrequencyType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataFrequencyType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataProviderType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProviderType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Strategy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InstanceName = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Strategy", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Connection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BaseUrl = table.Column<string>(type: "text", nullable: true),
                    ApiKey = table.Column<string>(type: "text", nullable: true),
                    ApiSecretKey = table.Column<string>(type: "text", nullable: true),
                    IsDataSource = table.Column<bool>(type: "boolean", nullable: true),
                    ConnectionTypeId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Connection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Connection_ConnectionType_ConnectionTypeId",
                        column: x => x.ConnectionTypeId,
                        principalTable: "ConnectionType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DataProvider",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsLive = table.Column<bool>(type: "boolean", nullable: false),
                    Start = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    End = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DataProviderTypeId = table.Column<int>(type: "integer", nullable: true),
                    DataFormatTypeId = table.Column<int>(type: "integer", nullable: true),
                    DataFrequencyTypeId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProvider", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataProvider_DataFormatType_DataFormatTypeId",
                        column: x => x.DataFormatTypeId,
                        principalTable: "DataFormatType",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DataProvider_DataFrequencyType_DataFrequencyTypeId",
                        column: x => x.DataFrequencyTypeId,
                        principalTable: "DataFrequencyType",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DataProvider_DataProviderType_DataProviderTypeId",
                        column: x => x.DataProviderTypeId,
                        principalTable: "DataProviderType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Backtest",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Start = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    End = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConnectionId = table.Column<int>(type: "integer", nullable: true),
                    DataProviderId = table.Column<int>(type: "integer", nullable: true),
                    StrategyId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Backtest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Backtest_Connection_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "Connection",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Backtest_DataProvider_DataProviderId",
                        column: x => x.DataProviderId,
                        principalTable: "DataProvider",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Backtest_Strategy_StrategyId",
                        column: x => x.StrategyId,
                        principalTable: "Strategy",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "ConnectionType",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "LastModifiedAt", "Name", "Notes" },
                values: new object[,]
                {
                    { 1, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "unknown connection type", true, null, "unknown", null },
                    { 2, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Connection properties for a 3rd party API", true, null, "api", null }
                });

            migrationBuilder.InsertData(
                table: "DataFormatType",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "LastModifiedAt", "Name", "Notes" },
                values: new object[] { 1, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Open, High, Low, Close", true, null, "OHLC", null });

            migrationBuilder.InsertData(
                table: "DataFrequencyType",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "LastModifiedAt", "Name", "Notes" },
                values: new object[,]
                {
                    { 1, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Data point for every second", true, null, "second", null },
                    { 2, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Data point for every minute", true, null, "minute", null },
                    { 3, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Data point for every hour", true, null, "hour", null },
                    { 4, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Data point for every day", true, null, "day", null }
                });

            migrationBuilder.InsertData(
                table: "DataProviderType",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "LastModifiedAt", "Name", "Notes" },
                values: new object[,]
                {
                    { 1, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Imported data file", true, null, "File", null },
                    { 2, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "3rd party API connection", true, null, "API", null },
                    { 3, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "3rd party websocket connection", true, null, "Websocket", null },
                    { 4, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "3rd party LLM prompt responses", true, null, "LLM", null }
                });

            migrationBuilder.InsertData(
                table: "Connection",
                columns: new[] { "Id", "ApiKey", "ApiSecretKey", "BaseUrl", "ConnectionTypeId", "CreatedAt", "Description", "IsActive", "IsDataSource", "LastModifiedAt", "Name", "Notes" },
                values: new object[,]
                {
                    { 1, null, null, null, 1, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, true, null, "Binance US", null },
                    { 2, null, null, null, 1, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, true, null, "Coinbase", null },
                    { 3, null, null, null, 1, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, false, null, "Charles Schwab", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Backtest_ConnectionId",
                table: "Backtest",
                column: "ConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Backtest_DataProviderId",
                table: "Backtest",
                column: "DataProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Backtest_StrategyId",
                table: "Backtest",
                column: "StrategyId");

            migrationBuilder.CreateIndex(
                name: "IX_Connection_ConnectionTypeId",
                table: "Connection",
                column: "ConnectionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DataProvider_DataFormatTypeId",
                table: "DataProvider",
                column: "DataFormatTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DataProvider_DataFrequencyTypeId",
                table: "DataProvider",
                column: "DataFrequencyTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DataProvider_DataProviderTypeId",
                table: "DataProvider",
                column: "DataProviderTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Backtest");

            migrationBuilder.DropTable(
                name: "Connection");

            migrationBuilder.DropTable(
                name: "DataProvider");

            migrationBuilder.DropTable(
                name: "Strategy");

            migrationBuilder.DropTable(
                name: "ConnectionType");

            migrationBuilder.DropTable(
                name: "DataFormatType");

            migrationBuilder.DropTable(
                name: "DataFrequencyType");

            migrationBuilder.DropTable(
                name: "DataProviderType");
        }
    }
}
