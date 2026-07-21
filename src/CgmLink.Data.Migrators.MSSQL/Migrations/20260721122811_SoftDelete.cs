using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CgmLink.Data.Migrators.MSSQL.Migrations
{
    /// <inheritdoc />
    public partial class SoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Deleted",
                table: "meals",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Deleted",
                table: "ingredients",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Deleted",
                table: "meals");

            migrationBuilder.DropColumn(
                name: "Deleted",
                table: "ingredients");
        }
    }
}
