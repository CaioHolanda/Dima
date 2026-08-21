using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dima.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserRolesRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IdentityRole_IdentityUser_UserId",
                table: "IdentityRole");

            migrationBuilder.DropIndex(
                name: "IX_IdentityRole_UserId",
                table: "IdentityRole");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "IdentityRole");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "IdentityRole",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdentityRole_UserId",
                table: "IdentityRole",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_IdentityRole_IdentityUser_UserId",
                table: "IdentityRole",
                column: "UserId",
                principalTable: "IdentityUser",
                principalColumn: "Id");
        }
    }
}
