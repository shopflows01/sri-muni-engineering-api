using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SriMuniEngineering_Api.Migrations
{
    /// <inheritdoc />
    public partial class FixAdminPasswordHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                column: "PasswordHash",
                value: "$2a$11$4EZ9QdbSPoWe65XQAA2kLuWSWdXbs1ZbHQ4A3O8iv3Se6ogLgMOBW");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                column: "PasswordHash",
                value: "$2a$11$Gf7VU1F6CVPjE7LUrGEA4OJx3b2ZkXJtTCpYMZPf7vF6QI0sN5gXy");
        }
    }
}
