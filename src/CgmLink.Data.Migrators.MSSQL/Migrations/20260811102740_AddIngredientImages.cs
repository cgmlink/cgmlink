using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CgmLink.Data.Migrators.MSSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddIngredientImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "ingredients",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrl",
                table: "ingredients",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "ingredients");

            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                table: "ingredients");
        }
    }
}
