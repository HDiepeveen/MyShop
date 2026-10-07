using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyShop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MollieCustomerPaymentStarts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerUserId",
                table: "OnlinePaymentStarts",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerUserId",
                table: "OnlinePaymentStarts");
        }
    }
}
