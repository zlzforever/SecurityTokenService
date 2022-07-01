#nullable disable

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SecurityTokenService.Data.MySql.Migrations.PersistedGrant
{
    public partial class PersistedGrantInit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "identity_server_device_codes",
                columns: table => new
                {
                    user_code = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    device_code = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    subject_id = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    session_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    client_id = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    creation_time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    expiration = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    data = table.Column<string>(type: "longtext", maxLength: 50000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_identity_server_device_codes", x => x.user_code);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "identity_server_persisted_grants",
                columns: table => new
                {
                    key = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    subject_id = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    session_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    client_id = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    creation_time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    expiration = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    consumed_time = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    data = table.Column<string>(type: "longtext", maxLength: 50000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_identity_server_persisted_grants", x => x.key);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_identity_server_device_codes_device_code",
                table: "identity_server_device_codes",
                column: "device_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_identity_server_device_codes_expiration",
                table: "identity_server_device_codes",
                column: "expiration");

            migrationBuilder.CreateIndex(
                name: "IX_identity_server_persisted_grants_expiration",
                table: "identity_server_persisted_grants",
                column: "expiration");

            migrationBuilder.CreateIndex(
                name: "IX_identity_server_persisted_grants_subject_id_client_id_type",
                table: "identity_server_persisted_grants",
                columns: new[] { "subject_id", "client_id", "type" });

            migrationBuilder.CreateIndex(
                name: "IX_identity_server_persisted_grants_subject_id_session_id_type",
                table: "identity_server_persisted_grants",
                columns: new[] { "subject_id", "session_id", "type" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "identity_server_device_codes");

            migrationBuilder.DropTable(
                name: "identity_server_persisted_grants");
        }
    }
}
