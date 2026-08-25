using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CgmLink.Nutrition.Data.Migrators.MSSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddProductBrandsAndServingNutrients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Nutriments_Carbohydrates100g",
                table: "Products",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Nutriments_CarbohydratesServing",
                table: "Products",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Nutriments_Energy100g",
                table: "Products",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Nutriments_EnergyKcal100g",
                table: "Products",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Nutriments_EnergyKcalServing",
                table: "Products",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Nutriments_EnergyServing",
                table: "Products",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Nutriments_Fat100g",
                table: "Products",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Nutriments_FatServing",
                table: "Products",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Nutriments_Proteins100g",
                table: "Products",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Nutriments_ProteinsServing",
                table: "Products",
                type: "float",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductBrands",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProductId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductBrands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductBrands_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductBrands_ProductId",
                table: "ProductBrands",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductBrands");

            migrationBuilder.DropColumn(
                name: "Nutriments_Carbohydrates100g",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Nutriments_CarbohydratesServing",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Nutriments_Energy100g",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Nutriments_EnergyKcal100g",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Nutriments_EnergyKcalServing",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Nutriments_EnergyServing",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Nutriments_Fat100g",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Nutriments_FatServing",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Nutriments_Proteins100g",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Nutriments_ProteinsServing",
                table: "Products");
        }
    }
}
