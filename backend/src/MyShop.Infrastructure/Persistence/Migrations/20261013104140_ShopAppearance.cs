using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyShop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ShopAppearance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BackgroundColor",
                table: "CatalogSeoSettings",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PanelColor",
                table: "CatalogSeoSettings",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PrimaryColor",
                table: "CatalogSeoSettings",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TextColor",
                table: "CatalogSeoSettings",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "CatalogSeoSettings",
                keyColumn: "Id",
                keyValue: new Guid("a178498a-a53f-48fb-9650-d0f3d3108322"),
                columns: new[] { "BackgroundColor", "PanelColor", "PrimaryColor", "TextColor" },
                values: new object[] { "#f6f7f1", "#fffefa", "#1d5543", "#223c33" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackgroundColor",
                table: "CatalogSeoSettings");

            migrationBuilder.DropColumn(
                name: "PanelColor",
                table: "CatalogSeoSettings");

            migrationBuilder.DropColumn(
                name: "PrimaryColor",
                table: "CatalogSeoSettings");

            migrationBuilder.DropColumn(
                name: "TextColor",
                table: "CatalogSeoSettings");
        }
    }
}
