using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarmaneTracker.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddProfessionPlanTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProfessionPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Profession = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Realm = table.Column<int>(type: "INTEGER", nullable: false),
                    Faction = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfessionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlanSteps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProfessionPlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    FromSkill = table.Column<int>(type: "INTEGER", nullable: false),
                    ToSkill = table.Column<int>(type: "INTEGER", nullable: false),
                    RecipeName = table.Column<string>(type: "TEXT", nullable: false),
                    CraftWowItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    CraftCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanSteps_ProfessionPlans_ProfessionPlanId",
                        column: x => x.ProfessionPlanId,
                        principalTable: "ProfessionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StepReagents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlanStepId = table.Column<int>(type: "INTEGER", nullable: false),
                    WowItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    NameHint = table.Column<string>(type: "TEXT", nullable: false),
                    Qty = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StepReagents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StepReagents_PlanSteps_PlanStepId",
                        column: x => x.PlanStepId,
                        principalTable: "PlanSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanSteps_ProfessionPlanId_Order",
                table: "PlanSteps",
                columns: new[] { "ProfessionPlanId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StepReagents_PlanStepId",
                table: "StepReagents",
                column: "PlanStepId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StepReagents");

            migrationBuilder.DropTable(
                name: "PlanSteps");

            migrationBuilder.DropTable(
                name: "ProfessionPlans");
        }
    }
}
