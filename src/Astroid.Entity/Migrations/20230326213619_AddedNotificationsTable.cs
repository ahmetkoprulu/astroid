using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Astroid.Entity.Migrations
{
	/// <inheritdoc />
	public partial class AddedNotificationsTable : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "Notifications",
				columns: table => new
				{
					NotificationId = table.Column<Guid>(type: "uuid", nullable: false),
					UserId = table.Column<Guid>(type: "uuid", nullable: false),
					CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
					SentDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
					Channel = table.Column<short>(type: "smallint", nullable: false),
					Status = table.Column<short>(type: "smallint", nullable: false),
					Subject = table.Column<string>(type: "text", nullable: false),
					Content = table.Column<string>(type: "text", nullable: false),
					To = table.Column<string>(type: "text", nullable: false),
					Error = table.Column<string>(type: "text", nullable: true)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Notifications", x => x.NotificationId);
					table.ForeignKey(
						name: "FK_Notifications_Users_UserId",
						column: x => x.UserId,
						principalTable: "Users",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "IX_Notifications_UserId",
				table: "Notifications",
				column: "UserId");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "Notifications");
		}
	}
}
