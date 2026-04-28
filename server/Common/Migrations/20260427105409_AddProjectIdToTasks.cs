using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Common.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectIdToTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tasks_projects_ProjectId",
                table: "tasks");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                table: "tasks",
                newName: "project_id");

            migrationBuilder.RenameIndex(
                name: "IX_tasks_ProjectId",
                table: "tasks",
                newName: "IX_tasks_project_id");

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_projects_project_id",
                table: "tasks",
                column: "project_id",
                principalTable: "projects",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tasks_projects_project_id",
                table: "tasks");

            migrationBuilder.RenameColumn(
                name: "project_id",
                table: "tasks",
                newName: "ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_tasks_project_id",
                table: "tasks",
                newName: "IX_tasks_ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_projects_ProjectId",
                table: "tasks",
                column: "ProjectId",
                principalTable: "projects",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
