using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CgmLink.Nutrition.Caching.Ef.Migrators.MSSQL.Migrations
{
    /// <inheritdoc />
    public partial class RenameNutritionCachedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DataAsOf",
                table: "nutrition_products",
                newName: "CachedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CachedAt",
                table: "nutrition_products",
                newName: "DataAsOf");
        }
    }
}
