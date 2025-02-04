using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class RemoveOldLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "discord_players");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "discord_players",
                columns: table => new
                {
                    discord_players_id = table.Column<Guid>(type: "uuid", nullable: false),
                    discord_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    hash_key = table.Column<string>(type: "text", nullable: false),
                    ss14_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discord_players", x => x.discord_players_id);
                    table.UniqueConstraint("ak_discord_players_ss14_id", x => x.ss14_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_discord_players_discord_id",
                table: "discord_players",
                column: "discord_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_discord_players_discord_players_id",
                table: "discord_players",
                column: "discord_players_id",
                unique: true);
        }
    }
}
