using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dima.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdjustUserVoucherInteraction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "AssignedUserId",
                table: "Voucher",
                type: "BIGINT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "VARCHAR(160)",
                oldMaxLength: 160,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Voucher_AssignedUserId",
                table: "Voucher",
                column: "AssignedUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Voucher_IdentityUser_AssignedUserId",
                table: "Voucher",
                column: "AssignedUserId",
                principalTable: "IdentityUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Voucher_IdentityUser_AssignedUserId",
                table: "Voucher");

            migrationBuilder.DropIndex(
                name: "IX_Voucher_AssignedUserId",
                table: "Voucher");

            migrationBuilder.AlterColumn<string>(
                name: "AssignedUserId",
                table: "Voucher",
                type: "VARCHAR(160)",
                maxLength: 160,
                nullable: true,
                oldClrType: typeof(long),
                oldType: "BIGINT",
                oldNullable: true);
        }
    }
}
