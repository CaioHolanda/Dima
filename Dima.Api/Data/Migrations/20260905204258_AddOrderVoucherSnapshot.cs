using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dima.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderVoucherSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VoucherCodeSnapshot",
                table: "Order",
                type: "VARCHAR(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "VoucherDiscountTypeSnapshot",
                table: "Order",
                type: "SMALLINT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VoucherValueSnapshot",
                table: "Order",
                type: "DECIMAL(18,2)",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE o
                SET
                    o.[VoucherCodeSnapshot] =
                        v.[Code],

                    o.[VoucherDiscountTypeSnapshot] =
                        v.[DiscountType],

                    o.[VoucherValueSnapshot] =
                        v.[Value]

                FROM [Order] AS o
                INNER JOIN [Voucher] AS v
                    ON v.[Id] = o.[VoucherId]

                WHERE o.[VoucherId] IS NOT NULL;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Order_VoucherSnapshot_Consistency",
                table: "Order",
                sql: "(\r\n    [VoucherId] IS NULL\r\n    AND [VoucherCodeSnapshot] IS NULL\r\n    AND [VoucherDiscountTypeSnapshot] IS NULL\r\n    AND [VoucherValueSnapshot] IS NULL\r\n)\r\nOR\r\n(\r\n    [VoucherId] IS NOT NULL\r\n    AND [VoucherCodeSnapshot] IS NOT NULL\r\n    AND [VoucherDiscountTypeSnapshot] IS NOT NULL\r\n    AND [VoucherValueSnapshot] IS NOT NULL\r\n)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Order_VoucherSnapshot_DiscountType",
                table: "Order",
                sql: "[VoucherDiscountTypeSnapshot] IS NULL\r\nOR [VoucherDiscountTypeSnapshot] IN (1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Order_VoucherSnapshot_Value",
                table: "Order",
                sql: "[VoucherValueSnapshot] IS NULL\r\nOR [VoucherValueSnapshot] > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Order_VoucherSnapshot_Consistency",
                table: "Order");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Order_VoucherSnapshot_DiscountType",
                table: "Order");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Order_VoucherSnapshot_Value",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "VoucherCodeSnapshot",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "VoucherDiscountTypeSnapshot",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "VoucherValueSnapshot",
                table: "Order");
        }
    }
}
