using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SriMuniEngineering_Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVoucherEntrySystemAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VoucherEntries_CustomerLedgers_LedgerId",
                table: "VoucherEntries");

            migrationBuilder.RenameColumn(
                name: "LedgerId",
                table: "VoucherEntries",
                newName: "CustomerLedgerId");

            migrationBuilder.RenameIndex(
                name: "IX_VoucherEntries_LedgerId",
                table: "VoucherEntries",
                newName: "IX_VoucherEntries_CustomerLedgerId");

            migrationBuilder.AddColumn<int>(
                name: "SystemAccount",
                table: "VoucherEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_VoucherEntries_CustomerLedgers_CustomerLedgerId",
                table: "VoucherEntries",
                column: "CustomerLedgerId",
                principalTable: "CustomerLedgers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VoucherEntries_CustomerLedgers_CustomerLedgerId",
                table: "VoucherEntries");

            migrationBuilder.DropColumn(
                name: "SystemAccount",
                table: "VoucherEntries");

            migrationBuilder.RenameColumn(
                name: "CustomerLedgerId",
                table: "VoucherEntries",
                newName: "LedgerId");

            migrationBuilder.RenameIndex(
                name: "IX_VoucherEntries_CustomerLedgerId",
                table: "VoucherEntries",
                newName: "IX_VoucherEntries_LedgerId");

            migrationBuilder.AddForeignKey(
                name: "FK_VoucherEntries_CustomerLedgers_LedgerId",
                table: "VoucherEntries",
                column: "LedgerId",
                principalTable: "CustomerLedgers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
