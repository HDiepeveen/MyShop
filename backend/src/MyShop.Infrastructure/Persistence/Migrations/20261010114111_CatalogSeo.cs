using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyShop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogSeo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CatalogSeoSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Heading = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SeoTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogSeoSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductSeo",
                columns: table => new
                {
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeoTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SeoDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    WebAddress = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSeo", x => x.ProductId);
                    table.ForeignKey(
                        name: "FK_ProductSeo_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductWebAddresses",
                columns: table => new
                {
                    Address = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductWebAddresses", x => x.Address);
                    table.ForeignKey(
                        name: "FK_ProductWebAddresses_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "CatalogSeoSettings",
                columns: new[] { "Id", "Heading", "SeoTitle", "Version" },
                values: new object[] { new Guid("a178498a-a53f-48fb-9650-d0f3d3108322"), "Ontdek ons assortiment", "Assortiment · MyShop", new Guid("5509887c-93b7-4d70-93db-8151db89fa01") });

            migrationBuilder.CreateIndex(
                name: "IX_ProductWebAddresses_ProductId",
                table: "ProductWebAddresses",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatalogSeoSettings");

            migrationBuilder.DropTable(
                name: "ProductSeo");

            migrationBuilder.DropTable(
                name: "ProductWebAddresses");
        }
    }
}
