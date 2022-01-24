using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace SteamTrader.Infrastructure.Data.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TradeOffers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    From = table.Column<int>(type: "integer", nullable: false),
                    To = table.Column<int>(type: "integer", nullable: false),
                    FromPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    ToPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    Margin = table.Column<decimal>(type: "numeric", nullable: false),
                    GameId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeOffers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TradeOffers_DateTime",
                table: "TradeOffers",
                column: "DateTime");

            migrationBuilder.CreateIndex(
                name: "IX_TradeOffers_From",
                table: "TradeOffers",
                column: "From");

            migrationBuilder.CreateIndex(
                name: "IX_TradeOffers_Margin",
                table: "TradeOffers",
                column: "Margin");

            migrationBuilder.CreateIndex(
                name: "IX_TradeOffers_To",
                table: "TradeOffers",
                column: "To");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TradeOffers");
        }
    }
}
