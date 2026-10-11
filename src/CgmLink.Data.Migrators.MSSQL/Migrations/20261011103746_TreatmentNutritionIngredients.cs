using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CgmLink.Data.Migrators.MSSQL.Migrations
{
    /// <inheritdoc />
    public partial class TreatmentNutritionIngredients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "treatment_nutrition_ingredients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TreatmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NutritionIngredientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_treatment_nutrition_ingredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_treatment_nutrition_ingredients_nutrition_ingredients_NutritionIngredientId",
                        column: x => x.NutritionIngredientId,
                        principalTable: "nutrition_ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_treatment_nutrition_ingredients_nutrition_servings_ServingId",
                        column: x => x.ServingId,
                        principalTable: "nutrition_servings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_treatment_nutrition_ingredients_treatments_TreatmentId",
                        column: x => x.TreatmentId,
                        principalTable: "treatments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_treatment_nutrition_ingredients_NutritionIngredientId",
                table: "treatment_nutrition_ingredients",
                column: "NutritionIngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_treatment_nutrition_ingredients_ServingId",
                table: "treatment_nutrition_ingredients",
                column: "ServingId");

            migrationBuilder.CreateIndex(
                name: "IX_treatment_nutrition_ingredients_TreatmentId",
                table: "treatment_nutrition_ingredients",
                column: "TreatmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "treatment_nutrition_ingredients");
        }
    }
}
