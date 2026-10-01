using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyShop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OnlinePaymentStartSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddressLine",
                table: "OnlinePaymentStarts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "OnlinePaymentStarts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "OnlinePaymentStarts",
                type: "nchar(2)",
                fixedLength: true,
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "OnlinePaymentStarts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "OnlinePaymentStarts",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "OnlinePaymentStarts",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "OnlinePaymentStartLines",
                columns: table => new
                {
                    CheckoutToken = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    VariantName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnlinePaymentStartLines", x => new { x.CheckoutToken, x.Ordinal });
                    table.ForeignKey(
                        name: "FK_OnlinePaymentStartLines_OnlinePaymentStarts_CheckoutToken",
                        column: x => x.CheckoutToken,
                        principalTable: "OnlinePaymentStarts",
                        principalColumn: "CheckoutToken",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OnlinePaymentStartLines");

            migrationBuilder.DropColumn(
                name: "AddressLine",
                table: "OnlinePaymentStarts");

            migrationBuilder.DropColumn(
                name: "City",
                table: "OnlinePaymentStarts");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "OnlinePaymentStarts");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "OnlinePaymentStarts");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "OnlinePaymentStarts");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "OnlinePaymentStarts");
        }
    }
}
