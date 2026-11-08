using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MUnique.OpenMU.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddWebShopAccountOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WebShopAccountOperation",
                schema: "data",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationType = table.Column<int>(type: "integer", nullable: false),
                    Reference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CharacterName = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    NewCharacterName = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CharacterClassNumber = table.Column<byte>(type: "smallint", nullable: true),
                    Amount = table.Column<int>(type: "integer", nullable: true),
                    RequestedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebShopAccountOperation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebShopAccountOperation_Account_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "data",
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WebShopAccountOperation_AccountId_AppliedAt",
                schema: "data",
                table: "WebShopAccountOperation",
                columns: new[] { "AccountId", "AppliedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WebShopAccountOperation_Reference",
                schema: "data",
                table: "WebShopAccountOperation",
                column: "Reference",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WebShopAccountOperation",
                schema: "data");
        }
    }
}
