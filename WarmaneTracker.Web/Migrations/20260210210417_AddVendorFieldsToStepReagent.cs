using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarmaneTracker.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddVendorFieldsToStepReagent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVendor",
                table: "StepReagents",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "VendorPriceCopper",
                table: "StepReagents",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVendor",
                table: "StepReagents");

            migrationBuilder.DropColumn(
                name: "VendorPriceCopper",
                table: "StepReagents");
        }
    }
}
