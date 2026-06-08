using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aristokeides.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPullRequestReviewComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PullRequestReviewComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PullRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<int>(type: "integer", nullable: false),
                    FilePath = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    OldLineNumber = table.Column<int>(type: "integer", nullable: true),
                    NewLineNumber = table.Column<int>(type: "integer", nullable: true),
                    LineType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DiffHunk = table.Column<string>(type: "text", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: false),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PullRequestReviewComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PullRequestReviewComments_PullRequestReviewComments_ParentId",
                        column: x => x.ParentId,
                        principalTable: "PullRequestReviewComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PullRequestReviewComments_PullRequests_PullRequestId",
                        column: x => x.PullRequestId,
                        principalTable: "PullRequests",
                        principalColumn: "IssueId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PullRequestReviewComments_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PullRequestReviewComments_AuthorId",
                table: "PullRequestReviewComments",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_PullRequestReviewComments_ParentId",
                table: "PullRequestReviewComments",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_PullRequestReviewComments_PullRequestId",
                table: "PullRequestReviewComments",
                column: "PullRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PullRequestReviewComments");
        }
    }
}
