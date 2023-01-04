using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Astroid.Entity.Migrations
{
    /// <inheritdoc />
    public partial class RenamedNameColumnToLabel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Bots");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Exchanges",
                newName: "Label");

            migrationBuilder.AddColumn<string>(
                name: "Label",
                table: "Bots",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Label",
                table: "Bots");

            migrationBuilder.RenameColumn(
                name: "Label",
                table: "Exchanges",
                newName: "Name");

            migrationBuilder.AddColumn<Guid>(
                name: "Name",
                table: "Bots",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
