using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarmaneTracker.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddTargetStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TargetStock",
                table: "Items",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TargetStock",
                table: "Items");
        }
    }
}
