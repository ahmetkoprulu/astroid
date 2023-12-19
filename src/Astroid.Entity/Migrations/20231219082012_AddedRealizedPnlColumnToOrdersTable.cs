using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Astroid.Entity.Migrations
{
    /// <inheritdoc />
    public partial class AddedRealizedPnlColumnToOrdersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RealizedPnl",
                table: "Orders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RealizedPnl",
                table: "Orders");
        }
    }
}
