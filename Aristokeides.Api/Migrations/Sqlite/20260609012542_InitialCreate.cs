using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aristokeides.Api.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Repositories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    OwnerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RequireSignedCommits = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPrivate = table.Column<bool>(type: "INTEGER", nullable: false),
                    PrimaryLanguage = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Repositories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Repositories_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SshKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    PublicKey = table.Column<string>(type: "text", nullable: false),
                    Fingerprint = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SshKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SshKeys_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BoardColumns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RepositoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardColumns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoardColumns_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommitSignatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CommitHash = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RepositoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SignerUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Algorithm = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    KeyFingerprint = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommitSignatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommitSignatures_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommitSignatures_Users_SignerUserId",
                        column: x => x.SignerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Issues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocalId = table.Column<int>(type: "INTEGER", nullable: false),
                    RepositoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CreatorId = table.Column<int>(type: "INTEGER", nullable: false),
                    AssigneeId = table.Column<int>(type: "INTEGER", nullable: true),
                    ColumnId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Issues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Issues_BoardColumns_ColumnId",
                        column: x => x.ColumnId,
                        principalTable: "BoardColumns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Issues_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Issues_Users_AssigneeId",
                        column: x => x.AssigneeId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Issues_Users_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IssueComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IssueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AuthorId = table.Column<int>(type: "INTEGER", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                    IssueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceBranch = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    TargetBranch = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    IsMerged = table.Column<bool>(type: "INTEGER", nullable: false),
                    MergeCommitSha = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true)
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

            migrationBuilder.CreateTable(
                name: "PullRequestReviewComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PullRequestId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AuthorId = table.Column<int>(type: "INTEGER", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    OldLineNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    NewLineNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    LineType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DiffHunk = table.Column<string>(type: "TEXT", nullable: true),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    IsResolved = table.Column<bool>(type: "INTEGER", nullable: false),
                    ParentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsPending = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsOutdated = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "PullRequestReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PullRequestId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AuthorId = table.Column<int>(type: "INTEGER", nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                name: "IX_BoardColumns_RepositoryId",
                table: "BoardColumns",
                column: "RepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CommitSignatures_RepositoryId_CommitHash",
                table: "CommitSignatures",
                columns: new[] { "RepositoryId", "CommitHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommitSignatures_SignerUserId",
                table: "CommitSignatures",
                column: "SignerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueComments_AuthorId",
                table: "IssueComments",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueComments_IssueId",
                table: "IssueComments",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_AssigneeId",
                table: "Issues",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_ColumnId",
                table: "Issues",
                column: "ColumnId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_CreatorId",
                table: "Issues",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_RepositoryId_LocalId",
                table: "Issues",
                columns: new[] { "RepositoryId", "LocalId" },
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_PullRequestReviews_AuthorId",
                table: "PullRequestReviews",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_PullRequestReviews_PullRequestId",
                table: "PullRequestReviews",
                column: "PullRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Repositories_OwnerId",
                table: "Repositories",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_SshKeys_Fingerprint",
                table: "SshKeys",
                column: "Fingerprint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SshKeys_UserId",
                table: "SshKeys",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommitSignatures");

            migrationBuilder.DropTable(
                name: "IssueComments");

            migrationBuilder.DropTable(
                name: "PullRequestReviewComments");

            migrationBuilder.DropTable(
                name: "PullRequestReviews");

            migrationBuilder.DropTable(
                name: "SshKeys");

            migrationBuilder.DropTable(
                name: "PullRequests");

            migrationBuilder.DropTable(
                name: "Issues");

            migrationBuilder.DropTable(
                name: "BoardColumns");

            migrationBuilder.DropTable(
                name: "Repositories");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
