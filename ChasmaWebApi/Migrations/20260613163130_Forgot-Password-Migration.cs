using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChasmaWebApi.Migrations
{
    /// <inheritdoc />
    public partial class ForgotPasswordMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "first_security_answer",
                table: "user_accounts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "first_security_answer_salt",
                table: "user_accounts",
                type: "BLOB",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "first_security_question",
                table: "user_accounts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "second_security_answer",
                table: "user_accounts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "second_security_answer_salt",
                table: "user_accounts",
                type: "BLOB",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "second_security_question",
                table: "user_accounts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "third_security_answer",
                table: "user_accounts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "third_security_answer_salt",
                table: "user_accounts",
                type: "BLOB",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "third_security_question",
                table: "user_accounts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "first_security_answer",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "first_security_answer_salt",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "first_security_question",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "second_security_answer",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "second_security_answer_salt",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "second_security_question",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "third_security_answer",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "third_security_answer_salt",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "third_security_question",
                table: "user_accounts");
        }
    }
}
