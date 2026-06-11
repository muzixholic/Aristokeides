using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aristokeides.Api.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddSshAuthLogSqlite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SshAuthLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AttemptedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClientIp = table.Column<string>(type: "TEXT", maxLength: 45, nullable: false),
                    KeyFingerprint = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    IsSuccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    FailureReason = table.Column<string>(type: "TEXT", nullable: true),
                    KeyType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SshAuthLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SshAuthLogs");
        }
    }
}
