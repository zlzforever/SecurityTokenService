﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecurityTokenService.Data.MySql.Migrations
{
    public partial class PersistedGrantInit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cerberus_device_codes",
                columns: table => new
                {
                    user_code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    device_code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    session_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    client_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    creation_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expiration = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data = table.Column<string>(type: "character varying(50000)", maxLength: 50000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cerberus_device_codes", x => x.user_code);
                });

            migrationBuilder.CreateTable(
                name: "cerberus_persisted_grants",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    session_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    client_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    creation_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expiration = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    consumed_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    data = table.Column<string>(type: "character varying(50000)", maxLength: 50000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cerberus_persisted_grants", x => x.key);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cerberus_device_codes_device_code",
                table: "cerberus_device_codes",
                column: "device_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cerberus_device_codes_expiration",
                table: "cerberus_device_codes",
                column: "expiration");

            migrationBuilder.CreateIndex(
                name: "IX_cerberus_persisted_grants_expiration",
                table: "cerberus_persisted_grants",
                column: "expiration");

            migrationBuilder.CreateIndex(
                name: "IX_cerberus_persisted_grants_subject_id_client_id_type",
                table: "cerberus_persisted_grants",
                columns: new[] { "subject_id", "client_id", "type" });

            migrationBuilder.CreateIndex(
                name: "IX_cerberus_persisted_grants_subject_id_session_id_type",
                table: "cerberus_persisted_grants",
                columns: new[] { "subject_id", "session_id", "type" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cerberus_device_codes");

            migrationBuilder.DropTable(
                name: "cerberus_persisted_grants");
        }
    }
}
