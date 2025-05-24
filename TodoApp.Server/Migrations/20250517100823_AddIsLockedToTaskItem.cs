using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApp.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddIsLockedToTaskItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "TaskItems",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "TaskItems");
        }
    }
}
