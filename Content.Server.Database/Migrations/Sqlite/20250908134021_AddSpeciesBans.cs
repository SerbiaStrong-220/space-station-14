using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddSpeciesBans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "server_species_ban",
                columns: table => new
                {
                    server_species_ban_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    round_id = table.Column<int>(type: "INTEGER", nullable: true),
                    player_user_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    playtime_at_note = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    address = table.Column<string>(type: "TEXT", nullable: true),
                    hwid = table.Column<byte[]>(type: "BLOB", nullable: true),
                    hwid_type = table.Column<int>(type: "INTEGER", nullable: true, defaultValue: 0),
                    ban_time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    expiration_time = table.Column<DateTime>(type: "TEXT", nullable: true),
                    reason = table.Column<string>(type: "TEXT", nullable: false),
                    severity = table.Column<int>(type: "INTEGER", nullable: false),
                    banning_admin = table.Column<Guid>(type: "TEXT", nullable: true),
                    last_edited_by_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    last_edited_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    hidden = table.Column<bool>(type: "INTEGER", nullable: false),
                    species_id = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server_species_ban", x => x.server_species_ban_id);
                    table.CheckConstraint("HaveEitherAddressOrUserIdOrHWId", "address IS NOT NULL OR player_user_id IS NOT NULL OR hwid IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_server_species_ban_player_banning_admin",
                        column: x => x.banning_admin,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_server_species_ban_player_last_edited_by_id",
                        column: x => x.last_edited_by_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_server_species_ban_round_round_id",
                        column: x => x.round_id,
                        principalTable: "round",
                        principalColumn: "round_id");
                });

            migrationBuilder.CreateTable(
                name: "server_species_unban",
                columns: table => new
                {
                    species_unban_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ban_id = table.Column<int>(type: "INTEGER", nullable: false),
                    unbanning_admin = table.Column<Guid>(type: "TEXT", nullable: true),
                    unban_time = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server_species_unban", x => x.species_unban_id);
                    table.ForeignKey(
                        name: "FK_server_species_unban_server_species_ban_ban_id",
                        column: x => x.ban_id,
                        principalTable: "server_species_ban",
                        principalColumn: "server_species_ban_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_server_species_ban_address",
                table: "server_species_ban",
                column: "address");

            migrationBuilder.CreateIndex(
                name: "IX_server_species_ban_banning_admin",
                table: "server_species_ban",
                column: "banning_admin");

            migrationBuilder.CreateIndex(
                name: "IX_server_species_ban_last_edited_by_id",
                table: "server_species_ban",
                column: "last_edited_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_server_species_ban_player_user_id",
                table: "server_species_ban",
                column: "player_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_server_species_ban_round_id",
                table: "server_species_ban",
                column: "round_id");

            migrationBuilder.CreateIndex(
                name: "IX_server_species_unban_ban_id",
                table: "server_species_unban",
                column: "ban_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "server_species_unban");

            migrationBuilder.DropTable(
                name: "server_species_ban");
        }
    }
}
