using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class TtsRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "voice",
                table: "profile");

            migrationBuilder.CreateTable(
                name: "tts_voice_preference",
                columns: table => new
                {
                    tts_voice_preference_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    profile_id = table.Column<int>(type: "integer", nullable: false),
                    position_index = table.Column<int>(type: "integer", nullable: false),
                    provider_name = table.Column<string>(type: "text", nullable: false),
                    voice_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tts_voice_preference", x => x.tts_voice_preference_id);
                    table.ForeignKey(
                        name: "FK_tts_voice_preference_profile_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profile",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tts_voice_preference_profile_id_position_index",
                table: "tts_voice_preference",
                columns: new[] { "profile_id", "position_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tts_voice_preference_profile_id_provider_name",
                table: "tts_voice_preference",
                columns: new[] { "profile_id", "provider_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tts_voice_preference_profile_id_voice_id",
                table: "tts_voice_preference",
                columns: new[] { "profile_id", "voice_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tts_voice_preference");

            migrationBuilder.AddColumn<string>(
                name: "voice",
                table: "profile",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
