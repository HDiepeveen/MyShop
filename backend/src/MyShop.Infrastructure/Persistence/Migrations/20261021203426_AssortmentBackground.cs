using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyShop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AssortmentBackground : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssortmentImageMode",
                table: "CatalogSeoSettings",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "CatalogSeoSettings",
                keyColumn: "Id",
                keyValue: new Guid("a178498a-a53f-48fb-9650-d0f3d3108322"),
                column: "AssortmentImageMode",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssortmentImageMode",
                table: "CatalogSeoSettings");
        }
    }
}
