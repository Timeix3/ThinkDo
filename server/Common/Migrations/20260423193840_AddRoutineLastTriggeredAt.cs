using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Common.Migrations
{
    /// <inheritdoc />
    public partial class AddRoutineLastTriggeredAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "last_triggered_at",
                table: "routines",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_routines_last_triggered_at",
                table: "routines",
                column: "last_triggered_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_routines_last_triggered_at",
                table: "routines");

            migrationBuilder.DropColumn(
                name: "last_triggered_at",
                table: "routines");
        }
    }
}
