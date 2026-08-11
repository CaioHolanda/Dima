using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dima.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameVoucherNumberToCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Voucher_Number",
                table: "Voucher");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Voucher_Number_Length",
                table: "Voucher");

            migrationBuilder.RenameColumn(
                name: "Number",
                table: "Voucher",
                newName: "Code");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Voucher",
                type: "VARCHAR(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "CHAR(8)",
                oldMaxLength: 8);

            migrationBuilder.CreateIndex(
                name: "IX_Voucher_Code",
                table: "Voucher",
                column: "Code",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Voucher_Code_Length",
                table: "Voucher",
                sql: "LEN([Code]) BETWEEN 4 AND 20");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Voucher_Code",
                table: "Voucher");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Voucher_Code_Length",
                table: "Voucher");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Voucher",
                type: "CHAR(8)",
                maxLength: 8,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "VARCHAR(20)",
                oldMaxLength: 20);

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "Voucher",
                newName: "Number");

            migrationBuilder.CreateIndex(
                name: "IX_Voucher_Number",
                table: "Voucher",
                column: "Number",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Voucher_Number_Length",
                table: "Voucher",
                sql: "LEN([Number]) = 8");
        }
    }
}
