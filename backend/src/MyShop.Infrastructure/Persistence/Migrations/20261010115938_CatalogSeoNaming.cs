using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyShop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogSeoNaming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductSeo_Products_ProductId",
                table: "ProductSeo");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductSeo",
                table: "ProductSeo");

            migrationBuilder.RenameTable(
                name: "ProductSeo",
                newName: "ProductSeos");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductSeos",
                table: "ProductSeos",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSeos_Products_ProductId",
                table: "ProductSeos",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductSeos_Products_ProductId",
                table: "ProductSeos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductSeos",
                table: "ProductSeos");

            migrationBuilder.RenameTable(
                name: "ProductSeos",
                newName: "ProductSeo");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductSeo",
                table: "ProductSeo",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSeo_Products_ProductId",
                table: "ProductSeo",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
