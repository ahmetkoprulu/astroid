using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Astroid.Entity.Migrations
{
    /// <inheritdoc />
    public partial class ReplacedClientIdWithRelatedToColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "Orders");

            migrationBuilder.AddColumn<Guid>(
                name: "RelatedTo",
                table: "Orders",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RelatedTo",
                table: "Orders");

            migrationBuilder.AddColumn<string>(
                name: "ClientId",
                table: "Orders",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
