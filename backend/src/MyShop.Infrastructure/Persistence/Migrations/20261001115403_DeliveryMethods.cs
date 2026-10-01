using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyShop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DeliveryMethods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryAmount",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryCurrency",
                table: "Orders",
                type: "nchar(3)",
                fixedLength: true,
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryDescription",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeliveryMethodId",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryMethodName",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeliveryMethods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryMethods", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryMethods_Name",
                table: "DeliveryMethods",
                column: "Name",
                unique: true);

            migrationBuilder.InsertData(
                table: "DeliveryMethods",
                columns: new[] { "Id", "Name", "Description", "Amount", "Currency", "Enabled", "Version" },
                values: new object[]
                {
                    new Guid("6c8d11ae-6c85-4fd2-8bc6-454779720001"),
                    "Standaardbezorging", "Bezorging op het opgegeven adres.", 0m, "EUR", true,
                    new Guid("6c8d11ae-6c85-4fd2-8bc6-454779720002")
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryMethods");

            migrationBuilder.DropColumn(
                name: "DeliveryAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryCurrency",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryDescription",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryMethodId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryMethodName",
                table: "Orders");
        }
    }
}
