using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dima.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderAccessDurationSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccessDurationMonths",
                table: "Order",
                type: "INT",
                nullable: true);

            migrationBuilder.Sql(
                """
        UPDATE o
        SET o.[AccessDurationMonths] =
            CASE
                WHEN o.[AccessStartsAt] IS NOT NULL
                     AND o.[AccessEndsAt] IS NOT NULL
                     AND DATEDIFF(
                         MONTH,
                         o.[AccessStartsAt],
                         o.[AccessEndsAt]) > 0
                THEN DATEDIFF(
                         MONTH,
                         o.[AccessStartsAt],
                         o.[AccessEndsAt])
                ELSE p.[AccessDurationMonths]
            END
        FROM [Order] AS o
        INNER JOIN [Product] AS p
            ON p.[Id] = o.[ProductId];
        """);

            migrationBuilder.AlterColumn<int>(
                name: "AccessDurationMonths",
                table: "Order",
                type: "INT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INT",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Order_AccessDurationMonths_Positive",
                table: "Order",
                sql: "[AccessDurationMonths] > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Order_AccessDurationMonths_Positive",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "AccessDurationMonths",
                table: "Order");
        }
    }
}
