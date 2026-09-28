using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CgmLink.Data.Migrators.MSSQL.Migrations
{
    /// <inheritdoc />
    public partial class UserOwnedIngredients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "ingredients",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(
                """
                ;WITH Owners AS
                (
                    SELECT IngredientId, UserId FROM user_ingredients
                    UNION
                    SELECT mi.IngredientId, m.UserId
                    FROM meal_ingredients mi
                    INNER JOIN meals m ON m.Id = mi.MealId
                    UNION
                    SELECT ti.IngredientId, t.UserId
                    FROM treatment_ingredients ti
                    INNER JOIN treatments t ON t.Id = ti.TreatmentId
                ),
                RankedOwners AS
                (
                    SELECT IngredientId, UserId,
                        ROW_NUMBER() OVER (PARTITION BY IngredientId ORDER BY UserId) AS OwnerRank
                    FROM Owners
                )
                SELECT IngredientId AS OldIngredientId, UserId,
                    CASE WHEN OwnerRank = 1 THEN IngredientId ELSE NEWID() END AS NewIngredientId,
                    OwnerRank
                INTO #IngredientOwnership
                FROM RankedOwners;

                UPDATE i
                SET UserId = ownership.UserId
                FROM ingredients i
                INNER JOIN #IngredientOwnership ownership
                    ON ownership.OldIngredientId = i.Id AND ownership.OwnerRank = 1;

                INSERT INTO ingredients
                    (Id, Name, ProductId, ImageUrl, ThumbnailUrl, Created, Updated, Deleted, UserId)
                SELECT ownership.NewIngredientId, i.Name, i.ProductId, i.ImageUrl, i.ThumbnailUrl,
                    i.Created, i.Updated, i.Deleted, ownership.UserId
                FROM #IngredientOwnership ownership
                INNER JOIN ingredients i ON i.Id = ownership.OldIngredientId
                WHERE ownership.OwnerRank > 1;

                SELECT s.Id AS OldServingId, ownership.OldIngredientId, ownership.UserId,
                    CASE WHEN ownership.OwnerRank = 1 THEN s.Id ELSE NEWID() END AS NewServingId,
                    ownership.NewIngredientId, ownership.OwnerRank
                INTO #ServingOwnership
                FROM ingredient_servings s
                INNER JOIN #IngredientOwnership ownership ON ownership.OldIngredientId = s.IngredientId;

                INSERT INTO ingredient_servings
                    (Id, IngredientId, ExternalId, Description, ServingAmount, ServingUnit,
                     Calories, Carbs, Protein, Fat, Created, Deleted)
                SELECT servings.NewServingId, servings.NewIngredientId, s.ExternalId, s.Description,
                    s.ServingAmount, s.ServingUnit, s.Calories, s.Carbs, s.Protein, s.Fat,
                    s.Created, s.Deleted
                FROM #ServingOwnership servings
                INNER JOIN ingredient_servings s ON s.Id = servings.OldServingId
                WHERE servings.OwnerRank > 1;

                UPDATE mi
                SET IngredientId = ownership.NewIngredientId,
                    ServingId = servings.NewServingId
                FROM meal_ingredients mi
                INNER JOIN meals m ON m.Id = mi.MealId
                INNER JOIN #IngredientOwnership ownership
                    ON ownership.OldIngredientId = mi.IngredientId AND ownership.UserId = m.UserId
                INNER JOIN #ServingOwnership servings
                    ON servings.OldIngredientId = mi.IngredientId
                    AND servings.OldServingId = mi.ServingId
                    AND servings.UserId = m.UserId;

                UPDATE ti
                SET IngredientId = ownership.NewIngredientId,
                    ServingId = servings.NewServingId
                FROM treatment_ingredients ti
                INNER JOIN treatments t ON t.Id = ti.TreatmentId
                INNER JOIN #IngredientOwnership ownership
                    ON ownership.OldIngredientId = ti.IngredientId AND ownership.UserId = t.UserId
                INNER JOIN #ServingOwnership servings
                    ON servings.OldIngredientId = ti.IngredientId
                    AND servings.OldServingId = ti.ServingId
                    AND servings.UserId = t.UserId;

                DELETE servings
                FROM ingredient_servings servings
                LEFT JOIN #IngredientOwnership ownership ON ownership.OldIngredientId = servings.IngredientId
                WHERE ownership.OldIngredientId IS NULL;

                DELETE ingredients
                FROM ingredients
                LEFT JOIN #IngredientOwnership ownership ON ownership.OldIngredientId = ingredients.Id
                WHERE ownership.OldIngredientId IS NULL;

                DROP TABLE #ServingOwnership;
                DROP TABLE #IngredientOwnership;
                """);

            migrationBuilder.DropTable(
                name: "user_ingredients");

            migrationBuilder.DropIndex(
                name: "IX_ingredients_Barcode",
                table: "ingredients");

            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "ingredients");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "ingredients",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ingredients_UserId",
                table: "ingredients",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ingredients_users_UserId",
                table: "ingredients",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ingredients_users_UserId",
                table: "ingredients");

            migrationBuilder.DropIndex(
                name: "IX_ingredients_UserId",
                table: "ingredients");

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "ingredients",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "user_ingredients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IngredientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_ingredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_ingredients_ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_ingredients_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ingredients_Barcode",
                table: "ingredients",
                column: "Barcode",
                unique: true,
                filter: "[Barcode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_user_ingredients_IngredientId",
                table: "user_ingredients",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_user_ingredients_UserId_IngredientId",
                table: "user_ingredients",
                columns: new[] { "UserId", "IngredientId" },
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO user_ingredients (Id, UserId, IngredientId, Created)
                SELECT NEWID(), UserId, Id, Created
                FROM ingredients;
                """);

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ingredients");
        }
    }
}
