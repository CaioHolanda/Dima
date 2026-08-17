using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dima.Api.Migrations
{
    /// <inheritdoc />
    public partial class LinkVoucherRedemptionToIdentityUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "UserId",
                table: "VoucherRedemption",
                type: "BIGINT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "VARCHAR(160)",
                oldMaxLength: 160);

            migrationBuilder.CreateIndex(
                name: "IX_VoucherRedemption_UserId",
                table: "VoucherRedemption",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_VoucherRedemption_IdentityUser_UserId",
                table: "VoucherRedemption",
                column: "UserId",
                principalTable: "IdentityUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VoucherRedemption_IdentityUser_UserId",
                table: "VoucherRedemption");

            migrationBuilder.DropIndex(
                name: "IX_VoucherRedemption_UserId",
                table: "VoucherRedemption");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "VoucherRedemption",
                type: "VARCHAR(160)",
                maxLength: 160,
                nullable: false,
                oldClrType: typeof(long),
                oldType: "BIGINT");
        }
    }
}
