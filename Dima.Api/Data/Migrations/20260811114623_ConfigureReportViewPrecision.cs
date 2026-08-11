using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dima.Api.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureReportViewPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<short>(
                name: "DiscountType",
                table: "Voucher",
                type: "SMALLINT",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "SMALLINT",
                oldDefaultValue: (short)1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<short>(
                name: "DiscountType",
                table: "Voucher",
                type: "SMALLINT",
                nullable: false,
                defaultValue: (short)1,
                oldClrType: typeof(short),
                oldType: "SMALLINT");
        }
    }
}
