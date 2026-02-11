using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharePointExternalUserManager.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAiAssistantTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiConversationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ConversationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Context = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssistantResponse = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TokensUsed = table.Column<int>(type: "int", nullable: false),
                    ResponseTimeMs = table.Column<int>(type: "int", nullable: false),
                    Model = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiConversationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiConversationLogs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AiSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    MarketingModeEnabled = table.Column<bool>(type: "bit", nullable: false),
                    InProductModeEnabled = table.Column<bool>(type: "bit", nullable: false),
                    MaxRequestsPerHour = table.Column<int>(type: "int", nullable: false),
                    MaxTokensPerRequest = table.Column<int>(type: "int", nullable: false),
                    MonthlyTokenBudget = table.Column<int>(type: "int", nullable: false),
                    TokensUsedThisMonth = table.Column<int>(type: "int", nullable: false),
                    LastMonthlyReset = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ShowDisclaimer = table.Column<bool>(type: "bit", nullable: false),
                    CustomSystemPrompt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiSettings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiConversationLogs_ConversationId",
                table: "AiConversationLogs",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_AiConversationLogs_TenantId_Mode",
                table: "AiConversationLogs",
                columns: new[] { "TenantId", "Mode" },
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AiConversationLogs_TenantId_Timestamp",
                table: "AiConversationLogs",
                columns: new[] { "TenantId", "Timestamp" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "UQ_AiSettings_TenantId",
                table: "AiSettings",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiConversationLogs");

            migrationBuilder.DropTable(
                name: "AiSettings");
        }
    }
}
