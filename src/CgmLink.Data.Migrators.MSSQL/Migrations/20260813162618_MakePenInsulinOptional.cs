using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CgmLink.Data.Migrators.MSSQL.Migrations
{
    /// <inheritdoc />
    public partial class MakePenInsulinOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_pens_insulin_InsulinId",
                table: "pens");

            migrationBuilder.AlterColumn<Guid>(
                name: "InsulinId",
                table: "pens",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddForeignKey(
                name: "FK_pens_insulin_InsulinId",
                table: "pens",
                column: "InsulinId",
                principalTable: "insulin",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_pens_insulin_InsulinId",
                table: "pens");

            migrationBuilder.AlterColumn<Guid>(
                name: "InsulinId",
                table: "pens",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_pens_insulin_InsulinId",
                table: "pens",
                column: "InsulinId",
                principalTable: "insulin",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
