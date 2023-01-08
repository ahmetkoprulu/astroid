using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Astroid.Entity.Migrations
{
    /// <inheritdoc />
    public partial class AddedKeyColumnToBotsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsStopLossActivated",
                table: "Bots",
                newName: "IsStopLossEnabled");

            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "Bots",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Key",
                table: "Bots");

            migrationBuilder.RenameColumn(
                name: "IsStopLossEnabled",
                table: "Bots",
                newName: "IsStopLossActivated");
        }
    }
}
