using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dima.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeOrderGatewayNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<short>(
                name: "Gateway",
                table: "Order",
                type: "SMALLINT",
                nullable: true,
                oldClrType: typeof(short),
                oldType: "SMALLINT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
              """
                UPDATE [Order]
                SET [Gateway] = 1
                WHERE [Gateway] IS NULL;
                """);
            migrationBuilder.AlterColumn<short>(
                name: "Gateway",
                table: "Order",
                type: "SMALLINT",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "SMALLINT",
                oldNullable: true);
        }
    }
}
