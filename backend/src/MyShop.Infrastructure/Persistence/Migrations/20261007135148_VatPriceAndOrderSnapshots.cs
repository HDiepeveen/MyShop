using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyShop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VatPriceAndOrderSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "NetPriceAmount",
                table: "ProductVariants",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "VatExempt",
                table: "ProductVariants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "VatRate",
                table: "ProductVariants",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NetAmount",
                table: "OrderLines",
                type: "decimal(20,2)",
                precision: 20,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VatAmount",
                table: "OrderLines",
                type: "decimal(20,2)",
                precision: 20,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "VatExempt",
                table: "OrderLines",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "VatRate",
                table: "OrderLines",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "VatExempt",
                table: "OnlinePaymentStartLines",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "VatRate",
                table: "OnlinePaymentStartLines",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NetPriceAmount",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "VatExempt",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "VatRate",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "NetAmount",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "VatAmount",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "VatExempt",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "VatRate",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "VatExempt",
                table: "OnlinePaymentStartLines");

            migrationBuilder.DropColumn(
                name: "VatRate",
                table: "OnlinePaymentStartLines");
        }
    }
}
