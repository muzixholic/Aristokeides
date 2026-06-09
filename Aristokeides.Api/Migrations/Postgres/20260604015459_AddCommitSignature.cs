using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aristokeides.Api.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddCommitSignature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequireSignedCommits",
                table: "Repositories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CommitSignatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CommitHash = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RepositoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    SignerUserId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Algorithm = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    KeyFingerprint = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_CommitSignatures_RepositoryId_CommitHash",
                table: "CommitSignatures",
                columns: new[] { "RepositoryId", "CommitHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommitSignatures_SignerUserId",
                table: "CommitSignatures",
                column: "SignerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommitSignatures");

            migrationBuilder.DropColumn(
                name: "RequireSignedCommits",
                table: "Repositories");
        }
    }
}
