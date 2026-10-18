using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
// SS220 pending bans

namespace Content.Server.Database.Migrations.Sqlite
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
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "pending_post_ban_info",
                table: "ban",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "pending_server",
                table: "ban",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "pending_trigger",
                table: "ban",
                type: "INTEGER",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<DateTime>(
                name: "pending_until",
                table: "ban",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(name: "IX_ban_pending_server", table: "ban", column: "pending_server");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
