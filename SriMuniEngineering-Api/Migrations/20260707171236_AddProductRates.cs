using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SriMuniEngineering_Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProductRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "GstPercent",
                table: "Products",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RatePerItem",
                table: "Products",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GstPercent",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RatePerItem",
                table: "Products");
        }
    }
}
