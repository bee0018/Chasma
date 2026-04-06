using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChasmaWebApi.Migrations
{
    /// <inheritdoc />
    public partial class RefreshTokenIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "refreshToken",
                table: "user_accounts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "refreshTokenExpiration",
                table: "user_accounts",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "refreshToken",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "refreshTokenExpiration",
                table: "user_accounts");
        }
    }
}
