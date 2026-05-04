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

            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS ""IX_tasks_ProjectId"";
DROP INDEX IF EXISTS ""IX_tasks_project_id"";
CREATE INDEX ""IX_tasks_project_id"" ON tasks (project_id);
");

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

            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS ""IX_tasks_project_id"";
DROP INDEX IF EXISTS ""IX_tasks_ProjectId"";
CREATE INDEX ""IX_tasks_ProjectId"" ON tasks (""ProjectId"");
");

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
