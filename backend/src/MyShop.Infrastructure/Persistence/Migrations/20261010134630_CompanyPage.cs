using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyShop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompanyPage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompanyAddress",
                table: "CatalogSeoSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyDescription",
                table: "CatalogSeoSettings",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyEmail",
                table: "CatalogSeoSettings",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyHeading",
                table: "CatalogSeoSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "CatalogSeoSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyOpeningHours",
                table: "CatalogSeoSettings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyPhone",
                table: "CatalogSeoSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FooterText",
                table: "CatalogSeoSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "CatalogSeoSettings",
                keyColumn: "Id",
                keyValue: new Guid("a178498a-a53f-48fb-9650-d0f3d3108322"),
                columns: new[] { "CompanyAddress", "CompanyDescription", "CompanyEmail", "CompanyHeading", "CompanyName", "CompanyOpeningHours", "CompanyPhone", "FooterText" },
                values: new object[] { null, null, null, "Over ons en contact", null, null, null, "Ontdek wat bij je past." });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyAddress",
                table: "CatalogSeoSettings");

            migrationBuilder.DropColumn(
                name: "CompanyDescription",
                table: "CatalogSeoSettings");

            migrationBuilder.DropColumn(
                name: "CompanyEmail",
                table: "CatalogSeoSettings");

            migrationBuilder.DropColumn(
                name: "CompanyHeading",
                table: "CatalogSeoSettings");

            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "CatalogSeoSettings");

            migrationBuilder.DropColumn(
                name: "CompanyOpeningHours",
                table: "CatalogSeoSettings");

            migrationBuilder.DropColumn(
                name: "CompanyPhone",
                table: "CatalogSeoSettings");

            migrationBuilder.DropColumn(
                name: "FooterText",
                table: "CatalogSeoSettings");
        }
    }
}
