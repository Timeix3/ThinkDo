using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Common.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskSprintSelection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_selected_for_sprint",
                table: "tasks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "ix_tasks_is_selected_for_sprint",
                table: "tasks",
                column: "is_selected_for_sprint");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tasks_is_selected_for_sprint",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "is_selected_for_sprint",
                table: "tasks");
        }
    }
}
