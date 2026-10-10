using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyShop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ShopBranding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Introduction",
                table: "CatalogSeoSettings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShopName",
                table: "CatalogSeoSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WelcomeText",
                table: "CatalogSeoSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "CatalogSeoSettings",
                keyColumn: "Id",
                keyValue: new Guid("a178498a-a53f-48fb-9650-d0f3d3108322"),
                columns: new[] { "Introduction", "ShopName", "WelcomeText" },
                values: new object[] { "Bekijk onze producten en kies de variant die bij je past.", "MyShop", "Welkom bij MyShop" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Introduction",
                table: "CatalogSeoSettings");

            migrationBuilder.DropColumn(
                name: "ShopName",
                table: "CatalogSeoSettings");

            migrationBuilder.DropColumn(
                name: "WelcomeText",
                table: "CatalogSeoSettings");
        }
    }
}
