using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarmaneTracker.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanStepNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlanStepNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlanStepId = table.Column<int>(type: "INTEGER", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    MinCharacterLevel = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanStepNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanStepNotes_PlanSteps_PlanStepId",
                        column: x => x.PlanStepId,
                        principalTable: "PlanSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanStepNotes_PlanStepId",
                table: "PlanStepNotes",
                column: "PlanStepId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanStepNotes");
        }
    }
}
