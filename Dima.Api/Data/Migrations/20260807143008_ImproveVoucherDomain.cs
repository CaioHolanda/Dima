using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dima.Api.Migrations
{
    /// <inheritdoc />
    public partial class ImproveVoucherDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Order_Product_ProductId",
                table: "Order");

            migrationBuilder.DropForeignKey(
                name: "FK_Order_Voucher_VoucherId",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "Voucher");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Voucher",
                type: "BIT",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "BIT");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Voucher",
                type: "NVARCHAR(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "NVARCHAR(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedUserId",
                table: "Voucher",
                type: "VARCHAR(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "DiscountType",
                table: "Voucher",
                type: "SMALLINT",
                nullable: false,
                defaultValue: (short)1);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndsAt",
                table: "Voucher",
                type: "DATETIME2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxTotalUses",
                table: "Voucher",
                type: "INT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxUsesPerUser",
                table: "Voucher",
                type: "INT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ProductId",
                table: "Voucher",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartsAt",
                table: "Voucher",
                type: "DATETIME2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Value",
                table: "Voucher",
                type: "DECIMAL(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "Order",
                type: "DECIMAL(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalPrice",
                table: "Order",
                type: "DECIMAL(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Total",
                table: "Order",
                type: "DECIMAL(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "VoucherRedemption",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VoucherId = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "VARCHAR(160)", maxLength: 160, nullable: false),
                    Status = table.Column<short>(type: "SMALLINT", nullable: false),
                    ReservedAt = table.Column<DateTime>(type: "DATETIME2", nullable: false),
                    RedeemedAt = table.Column<DateTime>(type: "DATETIME2", nullable: true),
                    ReleasedAt = table.Column<DateTime>(type: "DATETIME2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoucherRedemption", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VoucherRedemption_Order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Order",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VoucherRedemption_Voucher_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Voucher",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Voucher_Number",
                table: "Voucher",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Voucher_ProductId",
                table: "Voucher",
                column: "ProductId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Voucher_DiscountType",
                table: "Voucher",
                sql: "[DiscountType] IN (1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Voucher_MaxTotalUses",
                table: "Voucher",
                sql: "[MaxTotalUses] IS NULL OR [MaxTotalUses] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Voucher_MaxUsesPerUser",
                table: "Voucher",
                sql: "[MaxUsesPerUser] IS NULL OR [MaxUsesPerUser] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Voucher_Number_Length",
                table: "Voucher",
                sql: "LEN([Number]) = 8");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Voucher_Percentage_Range",
                table: "Voucher",
                sql: "[DiscountType] <> 2 OR [Value] <= 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Voucher_Validity",
                table: "Voucher",
                sql: "[StartsAt] IS NULL OR [EndsAt] IS NULL OR [EndsAt] > [StartsAt]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Voucher_Value_Positive",
                table: "Voucher",
                sql: "[Value] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_Order_Number",
                table: "Order",
                column: "Number",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Order_Discount_NotGreaterThanPrice",
                table: "Order",
                sql: "[DiscountAmount] <= [OriginalPrice]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Order_DiscountAmount_NonNegative",
                table: "Order",
                sql: "[DiscountAmount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Order_OriginalPrice_NonNegative",
                table: "Order",
                sql: "[OriginalPrice] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Order_Total_Calculation",
                table: "Order",
                sql: "[Total] = [OriginalPrice] - [DiscountAmount]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Order_Total_NonNegative",
                table: "Order",
                sql: "[Total] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_VoucherRedemption_OrderId",
                table: "VoucherRedemption",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoucherRedemption_VoucherId_UserId_Status",
                table: "VoucherRedemption",
                columns: new[] { "VoucherId", "UserId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_Order_Product_ProductId",
                table: "Order",
                column: "ProductId",
                principalTable: "Product",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Order_Voucher_VoucherId",
                table: "Order",
                column: "VoucherId",
                principalTable: "Voucher",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Voucher_Product_ProductId",
                table: "Voucher",
                column: "ProductId",
                principalTable: "Product",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Order_Product_ProductId",
                table: "Order");

            migrationBuilder.DropForeignKey(
                name: "FK_Order_Voucher_VoucherId",
                table: "Order");

            migrationBuilder.DropForeignKey(
                name: "FK_Voucher_Product_ProductId",
                table: "Voucher");

            migrationBuilder.DropTable(
                name: "VoucherRedemption");

            migrationBuilder.DropIndex(
                name: "IX_Voucher_Number",
                table: "Voucher");

            migrationBuilder.DropIndex(
                name: "IX_Voucher_ProductId",
                table: "Voucher");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Voucher_DiscountType",
                table: "Voucher");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Voucher_MaxTotalUses",
                table: "Voucher");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Voucher_MaxUsesPerUser",
                table: "Voucher");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Voucher_Number_Length",
                table: "Voucher");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Voucher_Percentage_Range",
                table: "Voucher");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Voucher_Validity",
                table: "Voucher");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Voucher_Value_Positive",
                table: "Voucher");

            migrationBuilder.DropIndex(
                name: "IX_Order_Number",
                table: "Order");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Order_Discount_NotGreaterThanPrice",
                table: "Order");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Order_DiscountAmount_NonNegative",
                table: "Order");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Order_OriginalPrice_NonNegative",
                table: "Order");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Order_Total_Calculation",
                table: "Order");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Order_Total_NonNegative",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "AssignedUserId",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "EndsAt",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "MaxTotalUses",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "MaxUsesPerUser",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "StartsAt",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "OriginalPrice",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "Total",
                table: "Order");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Voucher",
                type: "BIT",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "BIT",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Voucher",
                type: "NVARCHAR(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "Voucher",
                type: "MONEY",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddForeignKey(
                name: "FK_Order_Product_ProductId",
                table: "Order",
                column: "ProductId",
                principalTable: "Product",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Order_Voucher_VoucherId",
                table: "Order",
                column: "VoucherId",
                principalTable: "Voucher",
                principalColumn: "Id");
        }
    }
}
