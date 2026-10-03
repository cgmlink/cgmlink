using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CgmLink.Nutrition.Caching.Ef.Migrators.MSSQL.Migrations
{
    /// <inheritdoc />
    public partial class NutritionRetrievalTimestamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DataAsOf",
                table: "nutrition_products",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataAsOf",
                table: "nutrition_products");
        }
    }
}
