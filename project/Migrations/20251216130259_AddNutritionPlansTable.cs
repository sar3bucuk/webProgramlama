using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace proje.Migrations
{
    /// <inheritdoc />
    public partial class AddNutritionPlansTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NutritionPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberId = table.Column<int>(type: "int", nullable: false),
                    Goal = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DailyCalorieTarget = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    DailyProtein = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    DailyCarbohydrate = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    DailyFat = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    PlanDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SpecialNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Allergies = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DislikedFoods = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ActivityLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutritionPlans", x => x.Id);
                    table.CheckConstraint("CK_NutritionPlans_CalorieTarget", "[DailyCalorieTarget] > 0");
                    table.CheckConstraint("CK_NutritionPlans_Carbohydrate", "[DailyCarbohydrate] > 0");
                    table.CheckConstraint("CK_NutritionPlans_Fat", "[DailyFat] > 0");
                    table.CheckConstraint("CK_NutritionPlans_Protein", "[DailyProtein] > 0");
                    table.ForeignKey(
                        name: "FK_NutritionPlans_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NutritionPlans_CreatedDate",
                table: "NutritionPlans",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_NutritionPlans_MemberId",
                table: "NutritionPlans",
                column: "MemberId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NutritionPlans");
        }
    }
}
