using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aristokeides.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase11_AddRepoDashboardFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPrivate",
                table: "Repositories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryLanguage",
                table: "Repositories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Repositories",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPrivate",
                table: "Repositories");

            migrationBuilder.DropColumn(
                name: "PrimaryLanguage",
                table: "Repositories");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Repositories");
        }
    }
}
