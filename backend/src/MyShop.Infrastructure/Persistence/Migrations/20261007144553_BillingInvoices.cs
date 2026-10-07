using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MyShop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BillingInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanySettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AddressLine = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VatId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    KvkNumber = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    InvoicePrefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanySettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceCounters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NextNumber = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceCounters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Document = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VatRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Exempt = table.Column<bool>(type: "bit", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VatRates", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "CompanySettings",
                columns: new[] { "Id", "AddressLine", "City", "InvoicePrefix", "KvkNumber", "Name", "PostalCode", "VatId", "Version" },
                values: new object[] { new Guid("36d28057-9e9b-4fb7-8b13-c563b4813168"), "", "", "INV-", "", "", "", "", new Guid("184b4fba-f5ec-468b-b289-a6a43ef8c85c") });

            migrationBuilder.InsertData(
                table: "InvoiceCounters",
                columns: new[] { "Id", "NextNumber" },
                values: new object[] { new Guid("8f5f305d-2fd4-4b87-bb49-477655e24a91"), 1L });

            migrationBuilder.InsertData(
                table: "VatRates",
                columns: new[] { "Id", "Enabled", "Exempt", "Name", "Percentage", "Version" },
                values: new object[,]
                {
                    { new Guid("3db8eaab-ed19-4a15-90f1-9824cfc94800"), true, false, "0%", 0m, new Guid("751ae315-2fa8-4b94-a4f4-60ef0525c800") },
                    { new Guid("3db8eaab-ed19-4a15-90f1-9824cfc94801"), true, true, "Vrijgesteld", 0m, new Guid("751ae315-2fa8-4b94-a4f4-60ef0525c801") },
                    { new Guid("3db8eaab-ed19-4a15-90f1-9824cfc94809"), true, false, "9%", 9m, new Guid("751ae315-2fa8-4b94-a4f4-60ef0525c809") },
                    { new Guid("3db8eaab-ed19-4a15-90f1-9824cfc94821"), true, false, "21%", 21m, new Guid("751ae315-2fa8-4b94-a4f4-60ef0525c821") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Number",
                table: "Invoices",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_OrderId",
                table: "Invoices",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VatRates_Percentage_Exempt",
                table: "VatRates",
                columns: new[] { "Percentage", "Exempt" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanySettings");

            migrationBuilder.DropTable(
                name: "InvoiceCounters");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "VatRates");
        }
    }
}
