using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace egibi_api.Migrations
{
    /// <inheritdoc />
    public partial class addbackteststrategyrelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StrategyId",
                table: "Backtest",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Backtest_StrategyId",
                table: "Backtest",
                column: "StrategyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Backtest_Strategy_StrategyId",
                table: "Backtest",
                column: "StrategyId",
                principalTable: "Strategy",
                principalColumn: "StrategyID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Backtest_Strategy_StrategyId",
                table: "Backtest");

            migrationBuilder.DropIndex(
                name: "IX_Backtest_StrategyId",
                table: "Backtest");

            migrationBuilder.DropColumn(
                name: "StrategyId",
                table: "Backtest");
        }
    }
}
