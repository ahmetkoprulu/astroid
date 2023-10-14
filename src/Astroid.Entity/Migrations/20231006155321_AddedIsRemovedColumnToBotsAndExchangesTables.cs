using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Astroid.Entity.Migrations
{
    /// <inheritdoc />
    public partial class AddedIsRemovedColumnToBotsAndExchangesTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRemoved",
                table: "Exchanges",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRemoved",
                table: "Bots",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Bots_ExchangeId",
                table: "Bots",
                column: "ExchangeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bots_Exchanges_ExchangeId",
                table: "Bots",
                column: "ExchangeId",
                principalTable: "Exchanges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bots_Exchanges_ExchangeId",
                table: "Bots");

            migrationBuilder.DropIndex(
                name: "IX_Bots_ExchangeId",
                table: "Bots");

            migrationBuilder.DropColumn(
                name: "IsRemoved",
                table: "Exchanges");

            migrationBuilder.DropColumn(
                name: "IsRemoved",
                table: "Bots");
        }
    }
}
