using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChasmaWebApi.Migrations
{
    /// <inheritdoc />
    public partial class SystemManifestMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "system_manifests",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    version = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_manifests", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "system_manifests");
        }
    }
}
