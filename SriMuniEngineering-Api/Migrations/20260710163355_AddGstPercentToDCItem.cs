using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SriMuniEngineering_Api.Migrations
{
    /// <inheritdoc />
    public partial class AddGstPercentToDCItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "GstPercent",
                table: "JobWorkDCItems",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GstPercent",
                table: "JobWorkDCItems");
        }
    }
}
