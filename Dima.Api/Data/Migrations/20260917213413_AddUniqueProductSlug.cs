using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dima.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueProductSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT [Slug]
                    FROM [Product]
                    GROUP BY [Slug]
                    HAVING COUNT(*) > 1
                )
                BEGIN
                    THROW 51019, 'DT-19: existem slugs duplicados em Product. Corrija os dados antes de criar UX_Product_Slug.', 1;
                END;
                """);

            migrationBuilder.CreateIndex(
                name: "UX_Product_Slug",
                table: "Product",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Product_Slug",
                table: "Product");
        }
    }
}
