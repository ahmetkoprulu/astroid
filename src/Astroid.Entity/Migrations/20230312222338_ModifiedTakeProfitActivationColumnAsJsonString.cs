using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Astroid.Entity.Migrations
{
    /// <inheritdoc />
    public partial class ModifiedTakeProfitActivationColumnAsJsonString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfitActivation",
                table: "Bots");

            migrationBuilder.AddColumn<string>(
                name: "TakeProfitTargets",
                table: "Bots",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TakeProfitTargets",
                table: "Bots");

            migrationBuilder.AddColumn<decimal>(
                name: "ProfitActivation",
                table: "Bots",
                type: "numeric",
                nullable: true);
        }
    }
}
