using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyShop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StorePayLaterInstructions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PayLaterInstructions",
                table: "PaymentOptions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentInstructions",
                table: "Orders",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "PaymentOptions",
                keyColumn: "Id",
                keyValue: new Guid("5d484f50-9c6e-4e67-b5e0-3615cdb869eb"),
                column: "PayLaterInstructions",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayLaterInstructions",
                table: "PaymentOptions");

            migrationBuilder.DropColumn(
                name: "PaymentInstructions",
                table: "Orders");
        }
    }
}
