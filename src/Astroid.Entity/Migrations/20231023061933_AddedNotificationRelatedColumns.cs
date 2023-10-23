using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Astroid.Entity.Migrations
{
    /// <inheritdoc />
    public partial class AddedNotificationRelatedColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NotificationId",
                table: "Notifications",
                newName: "Id");

            migrationBuilder.AddColumn<short>(
                name: "ChannelPreference",
                table: "Users",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<string>(
                name: "TelegramId",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpireDate",
                table: "Notifications",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsNotificationEnabled",
                table: "Bots",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChannelPreference",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TelegramId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ExpireDate",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "IsNotificationEnabled",
                table: "Bots");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Notifications",
                newName: "NotificationId");
        }
    }
}
