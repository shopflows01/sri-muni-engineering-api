using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SriMuniEngineering_Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceDcDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DcDate",
                table: "Invoices",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DcDate",
                table: "Invoices");
        }
    }
}
