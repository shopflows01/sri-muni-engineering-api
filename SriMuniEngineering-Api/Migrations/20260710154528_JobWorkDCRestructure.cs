using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SriMuniEngineering_Api.Migrations
{
    /// <inheritdoc />
    public partial class JobWorkDCRestructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InspectionReports_JobWorkLedgers_DcLedgerId",
                table: "InspectionReports");

            migrationBuilder.DropTable(
                name: "JobWorkLedgers");

            migrationBuilder.RenameColumn(
                name: "DcLedgerId",
                table: "InspectionReports",
                newName: "DcItemId");

            migrationBuilder.RenameIndex(
                name: "IX_InspectionReports_DcLedgerId",
                table: "InspectionReports",
                newName: "IX_InspectionReports_DcItemId");

            migrationBuilder.CreateTable(
                name: "JobWorkDCs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DcNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DcDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobWorkDCs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobWorkDCs_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JobWorkDCItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DcId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QtySent = table.Column<int>(type: "int", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobWorkDCItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobWorkDCItems_JobWorkDCs_DcId",
                        column: x => x.DcId,
                        principalTable: "JobWorkDCs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobWorkDCItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JobWorkTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DcItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    ReferenceNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobWorkTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobWorkTransactions_JobWorkDCItems_DcItemId",
                        column: x => x.DcItemId,
                        principalTable: "JobWorkDCItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobWorkDCItems_DcId",
                table: "JobWorkDCItems",
                column: "DcId");

            migrationBuilder.CreateIndex(
                name: "IX_JobWorkDCItems_ProductId",
                table: "JobWorkDCItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_JobWorkDCs_CustomerId",
                table: "JobWorkDCs",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_JobWorkTransactions_DcItemId",
                table: "JobWorkTransactions",
                column: "DcItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_InspectionReports_JobWorkDCItems_DcItemId",
                table: "InspectionReports",
                column: "DcItemId",
                principalTable: "JobWorkDCItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InspectionReports_JobWorkDCItems_DcItemId",
                table: "InspectionReports");

            migrationBuilder.DropTable(
                name: "JobWorkTransactions");

            migrationBuilder.DropTable(
                name: "JobWorkDCItems");

            migrationBuilder.DropTable(
                name: "JobWorkDCs");

            migrationBuilder.RenameColumn(
                name: "DcItemId",
                table: "InspectionReports",
                newName: "DcLedgerId");

            migrationBuilder.RenameIndex(
                name: "IX_InspectionReports_DcItemId",
                table: "InspectionReports",
                newName: "IX_InspectionReports_DcLedgerId");

            migrationBuilder.CreateTable(
                name: "JobWorkLedgers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DcDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DcNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InwardQty = table.Column<int>(type: "int", nullable: false),
                    OutwardQty = table.Column<int>(type: "int", nullable: false),
                    RejectedQty = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobWorkLedgers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobWorkLedgers_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobWorkLedgers_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobWorkLedgers_CustomerId",
                table: "JobWorkLedgers",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_JobWorkLedgers_ProductId",
                table: "JobWorkLedgers",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_InspectionReports_JobWorkLedgers_DcLedgerId",
                table: "InspectionReports",
                column: "DcLedgerId",
                principalTable: "JobWorkLedgers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
