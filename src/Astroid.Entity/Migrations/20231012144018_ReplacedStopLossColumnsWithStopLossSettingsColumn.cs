using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Astroid.Entity.Migrations
{
    /// <inheritdoc />
    public partial class ReplacedStopLossColumnsWithStopLossSettingsColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StopLossCallbackRate",
                table: "Bots");

            migrationBuilder.DropColumn(
                name: "StopLossPrice",
                table: "Bots");

            migrationBuilder.DropColumn(
                name: "StopLossType",
                table: "Bots");

            migrationBuilder.AddColumn<string>(
                name: "StopLossSettings",
                table: "Bots",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StopLossSettings",
                table: "Bots");

            migrationBuilder.AddColumn<decimal>(
                name: "StopLossCallbackRate",
                table: "Bots",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StopLossPrice",
                table: "Bots",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StopLossType",
                table: "Bots",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
