using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CgmLink.Data.Migrators.MSSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddTreatments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "injections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InsulinId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Units = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Updated = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_injections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_injections_insulin_InsulinId",
                        column: x => x.InsulinId,
                        principalTable: "insulin",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_injections_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "treatments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReadingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InjectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Calories = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Carbs = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Protein = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Fat = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Updated = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Deleted = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_treatments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_treatments_injections_InjectionId",
                        column: x => x.InjectionId,
                        principalTable: "injections",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_treatments_readings_ReadingId",
                        column: x => x.ReadingId,
                        principalTable: "readings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_treatments_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "treatment_ingredients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TreatmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IngredientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_treatment_ingredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_treatment_ingredients_ingredient_servings_ServingId",
                        column: x => x.ServingId,
                        principalTable: "ingredient_servings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_treatment_ingredients_ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "ingredients",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_treatment_ingredients_treatments_TreatmentId",
                        column: x => x.TreatmentId,
                        principalTable: "treatments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "treatment_meals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TreatmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MealId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_treatment_meals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_treatment_meals_meals_MealId",
                        column: x => x.MealId,
                        principalTable: "meals",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_treatment_meals_treatments_TreatmentId",
                        column: x => x.TreatmentId,
                        principalTable: "treatments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_injections_InsulinId",
                table: "injections",
                column: "InsulinId");

            migrationBuilder.CreateIndex(
                name: "IX_injections_UserId",
                table: "injections",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_treatment_ingredients_IngredientId",
                table: "treatment_ingredients",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_treatment_ingredients_ServingId",
                table: "treatment_ingredients",
                column: "ServingId");

            migrationBuilder.CreateIndex(
                name: "IX_treatment_ingredients_TreatmentId",
                table: "treatment_ingredients",
                column: "TreatmentId");

            migrationBuilder.CreateIndex(
                name: "IX_treatment_meals_MealId",
                table: "treatment_meals",
                column: "MealId");

            migrationBuilder.CreateIndex(
                name: "IX_treatment_meals_TreatmentId",
                table: "treatment_meals",
                column: "TreatmentId");

            migrationBuilder.CreateIndex(
                name: "IX_treatments_InjectionId",
                table: "treatments",
                column: "InjectionId");

            migrationBuilder.CreateIndex(
                name: "IX_treatments_ReadingId",
                table: "treatments",
                column: "ReadingId");

            migrationBuilder.CreateIndex(
                name: "IX_treatments_UserId",
                table: "treatments",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "treatment_ingredients");

            migrationBuilder.DropTable(
                name: "treatment_meals");

            migrationBuilder.DropTable(
                name: "treatments");

            migrationBuilder.DropTable(
                name: "injections");
        }
    }
}
