using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Common.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskStatusAndSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "blocked_by_task_id",
                table: "tasks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "tasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "tasks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_tasks_blocked_by_task_id",
                table: "tasks",
                column: "blocked_by_task_id");

            migrationBuilder.CreateIndex(
                name: "ix_tasks_deleted_at",
                table: "tasks",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_tasks_status",
                table: "tasks",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tasks_blocked_by_task_id",
                table: "tasks");

            migrationBuilder.DropIndex(
                name: "ix_tasks_deleted_at",
                table: "tasks");

            migrationBuilder.DropIndex(
                name: "ix_tasks_status",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "blocked_by_task_id",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "status",
                table: "tasks");
        }
    }
}
