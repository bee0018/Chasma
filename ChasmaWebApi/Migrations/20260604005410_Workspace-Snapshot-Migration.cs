using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChasmaWebApi.Migrations
{
    /// <inheritdoc />
    public partial class WorkspaceSnapshotMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "repository_snapshots",
                columns: table => new
                {
                    database_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    snapshot_id = table.Column<int>(type: "INTEGER", nullable: false),
                    repository_id = table.Column<string>(type: "TEXT", nullable: false),
                    branch_name = table.Column<string>(type: "TEXT", nullable: false),
                    commit_hash = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<string>(type: "TEXT", nullable: false),
                    stash_message = table.Column<string>(type: "TEXT", nullable: true),
                    intent_note = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_repository_snapshots", x => x.database_id);
                });

            migrationBuilder.CreateTable(
                name: "work_context_snapshots",
                columns: table => new
                {
                    snapshot_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<int>(type: "INTEGER", nullable: false),
                    display_name = table.Column<string>(type: "TEXT", nullable: false),
                    snapshot_note = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_context_snapshots", x => x.snapshot_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "repository_snapshots");

            migrationBuilder.DropTable(
                name: "work_context_snapshots");
        }
    }
}
