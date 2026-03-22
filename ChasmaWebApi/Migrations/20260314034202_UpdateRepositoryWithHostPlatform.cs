using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChasmaWebApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRepositoryWithHostPlatform : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "remote_host_platform",
                table: "repositories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "remote_host_platform",
                table: "repositories");
        }
    }
}
