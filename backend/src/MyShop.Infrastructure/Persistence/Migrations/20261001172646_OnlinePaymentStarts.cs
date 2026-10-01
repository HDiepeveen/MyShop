using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyShop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OnlinePaymentStarts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OnlinePaymentStarts",
                columns: table => new
                {
                    CheckoutToken = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PaymentReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProviderPaymentId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CheckoutUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeliveryMethodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveryMethodName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeliveryDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeliveryAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DeliveryCurrency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnlinePaymentStarts", x => x.CheckoutToken);
                });

            migrationBuilder.CreateTable(
                name: "OnlinePaymentStartTotals",
                columns: table => new
                {
                    CheckoutToken = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnlinePaymentStartTotals", x => new { x.CheckoutToken, x.Currency });
                    table.ForeignKey(
                        name: "FK_OnlinePaymentStartTotals_OnlinePaymentStarts_CheckoutToken",
                        column: x => x.CheckoutToken,
                        principalTable: "OnlinePaymentStarts",
                        principalColumn: "CheckoutToken",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OnlinePaymentStarts_PaymentReference",
                table: "OnlinePaymentStarts",
                column: "PaymentReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnlinePaymentStarts_ProviderPaymentId",
                table: "OnlinePaymentStarts",
                column: "ProviderPaymentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OnlinePaymentStartTotals");

            migrationBuilder.DropTable(
                name: "OnlinePaymentStarts");
        }
    }
}
