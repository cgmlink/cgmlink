using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CgmLink.Nutrition.Caching.Ef.Migrators.MSSQL.Migrations
{
    /// <inheritdoc />
    public partial class CompleteNutritionProductCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "nutrition_products",
                columns: table => new
                {
                    Source = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ProductId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nutrition_products", x => new { x.Source, x.ProductId });
                });

            migrationBuilder.CreateTable(
                name: "nutrition_servings",
                columns: table => new
                {
                    Source = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ProductId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ServingId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ServingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ServingUnit = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Calories = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Carbs = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Protein = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Fat = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nutrition_servings", x => new { x.Source, x.ProductId, x.ServingId });
                    table.ForeignKey(
                        name: "FK_nutrition_servings_nutrition_products_Source_ProductId",
                        columns: x => new { x.Source, x.ProductId },
                        principalTable: "nutrition_products",
                        principalColumns: new[] { "Source", "ProductId" },
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "nutrition_servings");

            migrationBuilder.DropTable(
                name: "nutrition_products");
        }
    }
}
