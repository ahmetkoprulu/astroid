using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Astroid.Entity.Migrations
{
    /// <inheritdoc />
    public partial class AddedPyramidingSettingsColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TakeProfitTargets",
                table: "Bots",
                newName: "TakeProfitSettings");

            migrationBuilder.RenameColumn(
                name: "PyramidingTargets",
                table: "Bots",
                newName: "PyramidingSettings");

            migrationBuilder.AddColumn<short>(
                name: "ConditionType",
                table: "Orders",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConditionType",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "TakeProfitSettings",
                table: "Bots",
                newName: "TakeProfitTargets");

            migrationBuilder.RenameColumn(
                name: "PyramidingSettings",
                table: "Bots",
                newName: "PyramidingTargets");
        }
    }
}
