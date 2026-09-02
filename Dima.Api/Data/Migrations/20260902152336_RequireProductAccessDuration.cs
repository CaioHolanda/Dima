using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dima.Api.Migrations
{
    /// <inheritdoc />
    public partial class RequireProductAccessDuration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Product_AccessDurationMonths_Positive",
                table: "Product");

            migrationBuilder.AlterColumn<int>(
                name: "AccessDurationMonths",
                table: "Product",
                type: "INT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INT",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Product_AccessDurationMonths_Positive",
                table: "Product",
                sql: "[AccessDurationMonths] > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Product_AccessDurationMonths_Positive",
                table: "Product");

            migrationBuilder.AlterColumn<int>(
                name: "AccessDurationMonths",
                table: "Product",
                type: "INT",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INT");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Product_AccessDurationMonths_Positive",
                table: "Product",
                sql: "[AccessDurationMonths] IS NULL OR [AccessDurationMonths] > 0");
        }
    }
}
