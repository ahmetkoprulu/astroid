using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Astroid.Entity.Migrations
{
    /// <inheritdoc />
    public partial class AddedStopPriceAndStopCallbackRateColumnsToBotsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StopLossCallbackRate",
                table: "Bots");

            migrationBuilder.DropColumn(
                name: "StopLossPrice",
                table: "Bots");
        }
    }
}
