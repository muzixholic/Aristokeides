using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aristokeides.Api.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class Phase5_PullRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IssueComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IssueId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IssueComments_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IssueComments_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PullRequests",
                columns: table => new
                {
                    IssueId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceBranch = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TargetBranch = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsMerged = table.Column<bool>(type: "boolean", nullable: false),
                    MergeCommitSha = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PullRequests", x => x.IssueId);
                    table.ForeignKey(
                        name: "FK_PullRequests_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IssueComments_AuthorId",
                table: "IssueComments",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueComments_IssueId",
                table: "IssueComments",
                column: "IssueId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IssueComments");

            migrationBuilder.DropTable(
                name: "PullRequests");
        }
    }
}
