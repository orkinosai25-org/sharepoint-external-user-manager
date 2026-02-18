using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharePointExternalUserManager.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantAuth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AccessToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TokenExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Scope = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConsentGrantedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ConsentGrantedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastTokenRefresh = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantAuth", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantAuth_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantAuth_TokenExpiry",
                table: "TenantAuth",
                column: "TokenExpiresAt",
                filter: "[TokenExpiresAt] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_TenantAuth_TenantId",
                table: "TenantAuth",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantAuth");
        }
    }
}
