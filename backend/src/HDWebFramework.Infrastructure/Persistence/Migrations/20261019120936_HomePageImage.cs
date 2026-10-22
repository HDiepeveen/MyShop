using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HDWebFramework.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HomePageImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "HomeImageBytes",
                table: "CatalogSeoSettings",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HomeImageContentType",
                table: "CatalogSeoSettings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "CatalogSeoSettings",
                keyColumn: "Id",
                keyValue: new Guid("a178498a-a53f-48fb-9650-d0f3d3108322"),
                columns: new[] { "HomeImageBytes", "HomeImageContentType" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HomeImageBytes",
                table: "CatalogSeoSettings");

            migrationBuilder.DropColumn(
                name: "HomeImageContentType",
                table: "CatalogSeoSettings");
        }
    }
}
