using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebhookTester.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WebhookEndpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    HasPassword = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                    LastRequestAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookEndpoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebhookRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WebhookId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Method = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    QueryString = table.Column<string>(type: "TEXT", nullable: true),
                    Headers = table.Column<string>(type: "TEXT", nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: true),
                    ContentType = table.Column<string>(type: "TEXT", nullable: true),
                    StatusCodeSentBack = table.Column<int>(type: "INTEGER", nullable: false),
                    ClientIp = table.Column<string>(type: "TEXT", nullable: true),
                    IsBodyBase64Encoded = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsBodyTruncated = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebhookRequests_WebhookEndpoints_WebhookId",
                        column: x => x.WebhookId,
                        principalTable: "WebhookEndpoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WebhookRequests_ReceivedAt",
                table: "WebhookRequests",
                column: "ReceivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookRequests_WebhookId",
                table: "WebhookRequests",
                column: "WebhookId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WebhookRequests");

            migrationBuilder.DropTable(
                name: "WebhookEndpoints");
        }
    }
}
