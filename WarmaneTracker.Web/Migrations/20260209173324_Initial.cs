using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarmaneTracker.Web.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    Faction = table.Column<int>(type: "INTEGER", nullable: false),
                    Realm = table.Column<int>(type: "INTEGER", nullable: false),
                    LastMedianBuyoutGold = table.Column<decimal>(type: "TEXT", nullable: true),
                    LastMinBuyoutGold = table.Column<decimal>(type: "TEXT", nullable: true),
                    LastQty = table.Column<int>(type: "INTEGER", nullable: true),
                    LastFetchedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MedianBuyoutGold = table.Column<decimal>(type: "TEXT", nullable: false),
                    MinBuyoutGold = table.Column<decimal>(type: "TEXT", nullable: false),
                    Qty = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceHistory_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Stock",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    OnHand = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stock", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stock_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Items_Url",
                table: "Items",
                column: "Url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistory_ItemId",
                table: "PriceHistory",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Stock_ItemId",
                table: "Stock",
                column: "ItemId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriceHistory");

            migrationBuilder.DropTable(
                name: "Stock");

            migrationBuilder.DropTable(
                name: "Items");
        }
    }
}
