using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddBanChatAndSpecie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ban_role_ban_ban_id",
                table: "ban_role");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ban_role",
                table: "ban_role");

            migrationBuilder.RenameTable(
                name: "ban_role",
                newName: "iban_role");

            migrationBuilder.RenameColumn(
                name: "ban_role_id",
                table: "iban_role",
                newName: "iban_role_id");

            migrationBuilder.RenameIndex(
                name: "IX_ban_role_role_type_role_id_ban_id",
                table: "iban_role",
                newName: "IX_iban_role_role_type_role_id_ban_id");

            migrationBuilder.RenameIndex(
                name: "IX_ban_role_ban_id",
                table: "iban_role",
                newName: "IX_iban_role_ban_id");

            migrationBuilder.AddColumn<bool>(
                name: "teleport_afk_to_cryo_storage",
                table: "profile",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "admin_name_in_ban_time",
                table: "ban",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "stated_round",
                table: "ban",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "role_type",
                table: "iban_role",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "role_id",
                table: "iban_role",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "chat",
                table: "iban_role",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "discriminator",
                table: "iban_role",
                type: "TEXT",
                maxLength: 13,
                nullable: false,
                defaultValue: "BanRole");

            migrationBuilder.AddColumn<string>(
                name: "specie_id",
                table: "iban_role",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_iban_role",
                table: "iban_role",
                column: "iban_role_id");

            migrationBuilder.CreateIndex(
                name: "IX_iban_role_chat_ban_id",
                table: "iban_role",
                columns: new[] { "chat", "ban_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_iban_role_specie_id_ban_id",
                table: "iban_role",
                columns: new[] { "specie_id", "ban_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_iban_role_ban_ban_id",
                table: "iban_role",
                column: "ban_id",
                principalTable: "ban",
                principalColumn: "ban_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_iban_role_ban_ban_id",
                table: "iban_role");

            migrationBuilder.DropPrimaryKey(
                name: "PK_iban_role",
                table: "iban_role");

            migrationBuilder.DropIndex(
                name: "IX_iban_role_chat_ban_id",
                table: "iban_role");

            migrationBuilder.DropIndex(
                name: "IX_iban_role_specie_id_ban_id",
                table: "iban_role");

            migrationBuilder.DropColumn(
                name: "teleport_afk_to_cryo_storage",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "admin_name_in_ban_time",
                table: "ban");

            migrationBuilder.DropColumn(
                name: "stated_round",
                table: "ban");

            migrationBuilder.DropColumn(
                name: "chat",
                table: "iban_role");

            migrationBuilder.DropColumn(
                name: "discriminator",
                table: "iban_role");

            migrationBuilder.DropColumn(
                name: "specie_id",
                table: "iban_role");

            migrationBuilder.RenameTable(
                name: "iban_role",
                newName: "ban_role");

            migrationBuilder.RenameColumn(
                name: "iban_role_id",
                table: "ban_role",
                newName: "ban_role_id");

            migrationBuilder.RenameIndex(
                name: "IX_iban_role_role_type_role_id_ban_id",
                table: "ban_role",
                newName: "IX_ban_role_role_type_role_id_ban_id");

            migrationBuilder.RenameIndex(
                name: "IX_iban_role_ban_id",
                table: "ban_role",
                newName: "IX_ban_role_ban_id");

            migrationBuilder.AlterColumn<string>(
                name: "role_type",
                table: "ban_role",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "role_id",
                table: "ban_role",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ban_role",
                table: "ban_role",
                column: "ban_role_id");

            migrationBuilder.AddForeignKey(
                name: "FK_ban_role_ban_ban_id",
                table: "ban_role",
                column: "ban_id",
                principalTable: "ban",
                principalColumn: "ban_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
