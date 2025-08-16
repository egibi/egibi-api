using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace egibi_api.Migrations
{
    /// <inheritdoc />
    public partial class addaccountdetailsentities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Account_AccountType_AccountTypeId",
                table: "Account");

            migrationBuilder.DropIndex(
                name: "IX_Account_AccountTypeId",
                table: "Account");

            migrationBuilder.RenameColumn(
                name: "AccountTypeId",
                table: "Account",
                newName: "AccountStatusDetailsId");

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "AccountType",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AccountApiDetailsId",
                table: "Account",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AccountDetailsId",
                table: "Account",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AccountFeeStructureDetailsId",
                table: "Account",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AccountSecurityDetailsId",
                table: "Account",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AccountApiDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountApiDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountApiDetails_Account_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AccountDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    User = table.Column<string>(type: "text", nullable: true),
                    Url = table.Column<string>(type: "text", nullable: true),
                    AccountTypeId = table.Column<int>(type: "integer", nullable: true),
                    AccountId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountDetails_AccountType_AccountTypeId",
                        column: x => x.AccountTypeId,
                        principalTable: "AccountType",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AccountDetails_Account_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AccountFeeStructureDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountFeeStructureDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountFeeStructureDetails_Account_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AccountSecurityDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "text", nullable: true),
                    Password = table.Column<string>(type: "text", nullable: true),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccountId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountSecurityDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountSecurityDetails_Account_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AccountStatusDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountStatusDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountStatusDetails_Account_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountApiDetails_AccountId",
                table: "AccountApiDetails",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountDetails_AccountId",
                table: "AccountDetails",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountDetails_AccountTypeId",
                table: "AccountDetails",
                column: "AccountTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountFeeStructureDetails_AccountId",
                table: "AccountFeeStructureDetails",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountSecurityDetails_AccountId",
                table: "AccountSecurityDetails",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountStatusDetails_AccountId",
                table: "AccountStatusDetails",
                column: "AccountId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountApiDetails");

            migrationBuilder.DropTable(
                name: "AccountDetails");

            migrationBuilder.DropTable(
                name: "AccountFeeStructureDetails");

            migrationBuilder.DropTable(
                name: "AccountSecurityDetails");

            migrationBuilder.DropTable(
                name: "AccountStatusDetails");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "AccountType");

            migrationBuilder.DropColumn(
                name: "AccountApiDetailsId",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "AccountDetailsId",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "AccountFeeStructureDetailsId",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "AccountSecurityDetailsId",
                table: "Account");

            migrationBuilder.RenameColumn(
                name: "AccountStatusDetailsId",
                table: "Account",
                newName: "AccountTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Account_AccountTypeId",
                table: "Account",
                column: "AccountTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Account_AccountType_AccountTypeId",
                table: "Account",
                column: "AccountTypeId",
                principalTable: "AccountType",
                principalColumn: "Id");
        }
    }
}
