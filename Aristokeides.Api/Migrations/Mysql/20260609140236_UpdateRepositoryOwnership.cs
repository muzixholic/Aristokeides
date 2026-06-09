using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aristokeides.Api.Migrations.Mysql
{
    /// <inheritdoc />
    public partial class UpdateRepositoryOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Repositories_OrganizationId",
                table: "Repositories");

            migrationBuilder.DropIndex(
                name: "IX_Repositories_OwnerId",
                table: "Repositories");

            migrationBuilder.AlterColumn<int>(
                name: "OwnerId",
                table: "Repositories",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_Repositories_OrganizationId_Name",
                table: "Repositories",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Repositories_OwnerId_Name",
                table: "Repositories",
                columns: new[] { "OwnerId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Repositories_OrganizationId_Name",
                table: "Repositories");

            migrationBuilder.DropIndex(
                name: "IX_Repositories_OwnerId_Name",
                table: "Repositories");

            migrationBuilder.AlterColumn<int>(
                name: "OwnerId",
                table: "Repositories",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Repositories_OrganizationId",
                table: "Repositories",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Repositories_OwnerId",
                table: "Repositories",
                column: "OwnerId");
        }
    }
}
