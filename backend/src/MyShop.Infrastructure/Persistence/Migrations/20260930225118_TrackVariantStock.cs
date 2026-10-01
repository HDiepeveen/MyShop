using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyShop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TrackVariantStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StockQuantity",
                table: "ProductVariants",
                type: "int",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductVariants_StockQuantity",
                table: "ProductVariants",
                sql: "[StockQuantity] IS NULL OR [StockQuantity] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductVariants_StockQuantity",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "StockQuantity",
                table: "ProductVariants");
        }
    }
}
