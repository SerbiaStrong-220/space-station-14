using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
// SS220 pending bans

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class PendingBans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "pending_erase",
                table: "ban",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "pending_post_ban_info",
                table: "ban",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "pending_server",
                table: "ban",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "pending_trigger",
                table: "ban",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<DateTime>(
                name: "pending_until",
                table: "ban",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(name: "IX_ban_pending_server", table: "ban", column: "pending_server");

            // Other servers sharing this database must kick matching players on activation, not scheduling.
            migrationBuilder.Sql("""
                CREATE TRIGGER notify_on_pending_server_ban_activation
                    AFTER UPDATE OF pending_server ON ban
                    FOR EACH ROW
                    WHEN (OLD.pending_server IS NOT NULL AND NEW.pending_server IS NULL AND NEW.type = 0)
                    EXECUTE FUNCTION send_server_ban_notification();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER notify_on_pending_server_ban_activation ON ban;");
            migrationBuilder.DropIndex(name: "IX_ban_pending_server", table: "ban");

            migrationBuilder.DropColumn(
                name: "pending_erase",
                table: "ban");

            migrationBuilder.DropColumn(
                name: "pending_post_ban_info",
                table: "ban");

            migrationBuilder.DropColumn(
                name: "pending_server",
                table: "ban");

            migrationBuilder.DropColumn(
                name: "pending_trigger",
                table: "ban");

            migrationBuilder.DropColumn(
                name: "pending_until",
                table: "ban");
        }
    }
}
