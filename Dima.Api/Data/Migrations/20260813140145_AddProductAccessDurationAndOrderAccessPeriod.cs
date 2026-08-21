using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dima.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProductAccessDurationAndOrderAccessPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccessDurationMonths",
                table: "Product",
                type: "INT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AccessEndsAt",
                table: "Order",
                type: "DATETIME2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AccessStartsAt",
                table: "Order",
                type: "DATETIME2",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Product_AccessDurationMonths_Positive",
                table: "Product",
                sql: "[AccessDurationMonths] IS NULL OR [AccessDurationMonths] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Order_AccessPeriod",
                table: "Order",
                sql: "[AccessStartsAt] IS NULL OR [AccessEndsAt] IS NULL OR [AccessEndsAt] > [AccessStartsAt]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Product_AccessDurationMonths_Positive",
                table: "Product");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Order_AccessPeriod",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "AccessDurationMonths",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "AccessEndsAt",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "AccessStartsAt",
                table: "Order");
        }
    }
}
