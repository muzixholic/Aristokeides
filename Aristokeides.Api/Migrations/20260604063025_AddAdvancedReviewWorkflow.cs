using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aristokeides.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancedReviewWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOutdated",
                table: "PullRequestReviewComments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPending",
                table: "PullRequestReviewComments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PullRequestReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PullRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PullRequestReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PullRequestReviews_PullRequests_PullRequestId",
                        column: x => x.PullRequestId,
                        principalTable: "PullRequests",
                        principalColumn: "IssueId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PullRequestReviews_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PullRequestReviews_AuthorId",
                table: "PullRequestReviews",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_PullRequestReviews_PullRequestId",
                table: "PullRequestReviews",
                column: "PullRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PullRequestReviews");

            migrationBuilder.DropColumn(
                name: "IsOutdated",
                table: "PullRequestReviewComments");

            migrationBuilder.DropColumn(
                name: "IsPending",
                table: "PullRequestReviewComments");
        }
    }
}
