using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevTools.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SnapshotEntityBase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app_settings",
                columns: table => new
                {
                    key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    value_json = table.Column<string>(type: "TEXT", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_settings", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "tool_configurations",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    tool_slug = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false),
                    is_default = table.Column<bool>(type: "INTEGER", nullable: false),
                    payload_json = table.Column<string>(type: "TEXT", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tool_configurations", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tool_cfg_tool_slug",
                table: "tool_configurations",
                column: "tool_slug");

            migrationBuilder.CreateIndex(
                name: "ix_tool_cfg_tool_slug_default",
                table: "tool_configurations",
                columns: new[] { "tool_slug", "is_default" });

            migrationBuilder.CreateIndex(
                name: "ux_tool_cfg_tool_slug_name",
                table: "tool_configurations",
                columns: new[] { "tool_slug", "name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_settings");

            migrationBuilder.DropTable(
                name: "tool_configurations");
        }
    }
}
