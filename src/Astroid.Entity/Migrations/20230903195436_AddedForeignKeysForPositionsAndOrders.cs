using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Astroid.Entity.Migrations
{
    /// <inheritdoc />
    public partial class AddedForeignKeysForPositionsAndOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EntryType",
                table: "Orders",
                newName: "TriggerType");

            migrationBuilder.AddColumn<bool>(
                name: "ClosePosition",
                table: "Orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                table: "Orders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Positions_BotId",
                table: "Positions",
                column: "BotId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_ExchangeId",
                table: "Positions",
                column: "ExchangeId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_UserId",
                table: "Positions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BotId",
                table: "Orders",
                column: "BotId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ExchangeId",
                table: "Orders",
                column: "ExchangeId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PositionId",
                table: "Orders",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId",
                table: "Orders",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Bots_BotId",
                table: "Orders",
                column: "BotId",
                principalTable: "Bots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Exchanges_ExchangeId",
                table: "Orders",
                column: "ExchangeId",
                principalTable: "Exchanges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Positions_PositionId",
                table: "Orders",
                column: "PositionId",
                principalTable: "Positions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_UserId",
                table: "Orders",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Positions_Bots_BotId",
                table: "Positions",
                column: "BotId",
                principalTable: "Bots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Positions_Exchanges_ExchangeId",
                table: "Positions",
                column: "ExchangeId",
                principalTable: "Exchanges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Positions_Users_UserId",
                table: "Positions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Bots_BotId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Exchanges_ExchangeId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Positions_PositionId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_UserId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Positions_Bots_BotId",
                table: "Positions");

            migrationBuilder.DropForeignKey(
                name: "FK_Positions_Exchanges_ExchangeId",
                table: "Positions");

            migrationBuilder.DropForeignKey(
                name: "FK_Positions_Users_UserId",
                table: "Positions");

            migrationBuilder.DropIndex(
                name: "IX_Positions_BotId",
                table: "Positions");

            migrationBuilder.DropIndex(
                name: "IX_Positions_ExchangeId",
                table: "Positions");

            migrationBuilder.DropIndex(
                name: "IX_Positions_UserId",
                table: "Positions");

            migrationBuilder.DropIndex(
                name: "IX_Orders_BotId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ExchangeId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_PositionId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_UserId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ClosePosition",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "TriggerType",
                table: "Orders",
                newName: "EntryType");
        }
    }
}
