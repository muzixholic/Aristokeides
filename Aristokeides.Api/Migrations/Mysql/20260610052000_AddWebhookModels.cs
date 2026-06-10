using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Aristokeides.Api.Migrations.Mysql
{
    /// <inheritdoc />
    public partial class AddWebhookModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Webhooks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    RepositoryId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Url = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false),
                    Secret = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    ContentType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    WebhookType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TriggerEvents = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Webhooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Webhooks_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WebhookDeliveries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    WebhookId = table.Column<int>(type: "int", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "char(36)", nullable: false),
                    EventType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    RequestHeaders = table.Column<string>(type: "longtext", nullable: true),
                    RequestBody = table.Column<string>(type: "longtext", nullable: true),
                    ResponseHeaders = table.Column<string>(type: "longtext", nullable: true),
                    ResponseBody = table.Column<string>(type: "longtext", nullable: true),
                    HttpStatusCode = table.Column<int>(type: "int", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    IsSuccess = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebhookDeliveries_Webhooks_WebhookId",
                        column: x => x.WebhookId,
                        principalTable: "Webhooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveries_WebhookId",
                table: "WebhookDeliveries",
                column: "WebhookId");

            migrationBuilder.CreateIndex(
                name: "IX_Webhooks_RepositoryId",
                table: "Webhooks",
                column: "RepositoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WebhookDeliveries");

            migrationBuilder.DropTable(
                name: "Webhooks");
        }
    }
}
