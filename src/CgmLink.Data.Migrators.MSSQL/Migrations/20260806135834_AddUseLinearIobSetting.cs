using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CgmLink.Data.Migrators.MSSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddUseLinearIobSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UseLinearIob",
                table: "user_settings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UseLinearIob",
                table: "user_settings");
        }
    }
}
