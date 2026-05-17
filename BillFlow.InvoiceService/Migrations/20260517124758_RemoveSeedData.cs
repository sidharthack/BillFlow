using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BillFlow.InvoiceService.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Invoices",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Invoices",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Invoices",
                keyColumn: "Id",
                keyValue: 3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Invoices",
                columns: new[] { "Id", "Amount", "CreatedAt", "CustomerName", "InvoiceNumber", "PaidAt", "SentAt", "Status", "TaxRate", "TenantId" },
                values: new object[,]
                {
                    { 1, 50000m, new DateTime(2026, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc), "Acme Corp", "INV-001", new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc), null, "Paid", 0.18m, 1 },
                    { 2, 120000m, new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "GlobalTech Ltd", "INV-002", null, null, "Sent", 0.18m, 1 },
                    { 3, 25000m, new DateTime(2026, 3, 5, 0, 0, 0, 0, DateTimeKind.Utc), "StartupXYZ", "INV-003", null, null, "Draft", 0.18m, 1 }
                });
        }
    }
}
