using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FnD.Cloud.API.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SyncTimestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClientMachineName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecordCountProcessed = table.Column<int>(type: "int", nullable: false),
                    DuplicateCountSkipped = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorDetails = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncLogs");
        }
    }
}
