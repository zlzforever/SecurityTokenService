using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecurityTokenService.Data.PostgreSql.Migrations.SecurityTokenService
{
    public partial class AddUserClaimPicture : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "picture",
                table: "cerberus_user",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "picture",
                table: "cerberus_user");
        }
    }
}
