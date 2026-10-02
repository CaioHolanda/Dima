using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dima.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundReasonToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "RefundReason",
                table: "Order",
                type: "SMALLINT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundReasonDetails",
                table: "Order",
                type: "NVARCHAR(500)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefundReason",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "RefundReasonDetails",
                table: "Order");
        }
    }
}
