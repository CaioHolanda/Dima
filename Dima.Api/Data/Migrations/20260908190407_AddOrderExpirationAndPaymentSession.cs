using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dima.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderExpirationAndPaymentSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiredAt",
                table: "Order",
                type: "DATETIMEOFFSET",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiresAt",
                table: "Order",
                type: "DATETIMEOFFSET",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PaymentSessionExpiresAt",
                table: "Order",
                type: "DATETIMEOFFSET",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentSessionId",
                table: "Order",
                type: "NVARCHAR(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Order_WaitingPayment_ExpiresAt",
                table: "Order",
                column: "ExpiresAt",
                filter: "[Status] = 1 AND [ExpiresAt] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Order_PaymentSessionId",
                table: "Order",
                column: "PaymentSessionId",
                unique: true,
                filter: "[PaymentSessionId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Order_WaitingPayment_ExpiresAt",
                table: "Order");

            migrationBuilder.DropIndex(
                name: "UX_Order_PaymentSessionId",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "ExpiredAt",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "PaymentSessionExpiresAt",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "PaymentSessionId",
                table: "Order");
        }
    }
}
