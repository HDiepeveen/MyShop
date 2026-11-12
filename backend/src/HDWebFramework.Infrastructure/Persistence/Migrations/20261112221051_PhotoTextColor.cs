using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HDWebFramework.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PhotoTextColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoTextColor",
                table: "CatalogSeoSettings",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#dbeafe");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoTextColor",
                table: "CatalogSeoSettings");
        }
    }
}
