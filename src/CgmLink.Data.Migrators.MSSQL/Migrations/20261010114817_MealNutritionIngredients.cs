using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CgmLink.Data.Migrators.MSSQL.Migrations
{
    /// <inheritdoc />
    public partial class MealNutritionIngredients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "ingredients");

            migrationBuilder.CreateTable(
                name: "meal_nutrition_ingredients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MealId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NutritionIngredientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meal_nutrition_ingredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_meal_nutrition_ingredients_meals_MealId",
                        column: x => x.MealId,
                        principalTable: "meals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_meal_nutrition_ingredients_nutrition_ingredients_NutritionIngredientId",
                        column: x => x.NutritionIngredientId,
                        principalTable: "nutrition_ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_meal_nutrition_ingredients_nutrition_servings_ServingId",
                        column: x => x.ServingId,
                        principalTable: "nutrition_servings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_meal_nutrition_ingredients_MealId",
                table: "meal_nutrition_ingredients",
                column: "MealId");

            migrationBuilder.CreateIndex(
                name: "IX_meal_nutrition_ingredients_NutritionIngredientId",
                table: "meal_nutrition_ingredients",
                column: "NutritionIngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_meal_nutrition_ingredients_ServingId",
                table: "meal_nutrition_ingredients",
                column: "ServingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "meal_nutrition_ingredients");

            migrationBuilder.AddColumn<string>(
                name: "ProductId",
                table: "ingredients",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
