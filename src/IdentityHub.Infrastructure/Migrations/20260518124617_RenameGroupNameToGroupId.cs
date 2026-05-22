using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameGroupNameToGroupId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GroupRoleMappings_GroupName",
                table: "GroupRoleMappings");

            migrationBuilder.DropColumn(
                name: "GroupName",
                table: "GroupRoleMappings");

            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                table: "GroupRoleMappings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_GroupRoleMappings_GroupId",
                table: "GroupRoleMappings",
                column: "GroupId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GroupRoleMappings_GroupId",
                table: "GroupRoleMappings");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "GroupRoleMappings");

            migrationBuilder.AddColumn<string>(
                name: "GroupName",
                table: "GroupRoleMappings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_GroupRoleMappings_GroupName",
                table: "GroupRoleMappings",
                column: "GroupName",
                unique: true);
        }
    }
}
