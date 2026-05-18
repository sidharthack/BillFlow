using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BillFlow.InvoiceService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Invoices",
                columns: new[] { "Id", "Amount", "CreatedAt", "CustomerName", "InvoiceNumber", "PaidAt", "SentAt", "Status", "TaxRate" },
                values: new object[,]
                {
                    { 1, 50000m, new DateTime(2026, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc), "Acme Corp", "INV-001", new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc), null, "Paid", 0.18m },
                    { 2, 120000m, new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "GlobalTech Ltd", "INV-002", null, null, "Sent", 0.18m },
                    { 3, 25000m, new DateTime(2026, 3, 5, 0, 0, 0, 0, DateTimeKind.Utc), "StartupXYZ", "INV-003", null, null, "Draft", 0.18m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Invoices");
        }
    }
}
