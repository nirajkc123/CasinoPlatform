using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasinoPlatform.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBlackjackAndRoulette : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlackjackRounds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BetAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PayoutAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PlayerCards = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DealerCards = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RemainingDeck = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlackjackRounds", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlackjackRounds_RequestId",
                table: "BlackjackRounds",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlackjackRounds_UserId",
                table: "BlackjackRounds",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlackjackRounds");
        }
    }
}
