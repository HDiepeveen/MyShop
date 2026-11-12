using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HDWebFramework.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AssortmentButtonOpacity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssortmentButtonOpacity",
                table: "CatalogSeoSettings",
                type: "int",
                nullable: false,
                defaultValue: 10);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssortmentButtonOpacity",
                table: "CatalogSeoSettings");
        }
    }
}
