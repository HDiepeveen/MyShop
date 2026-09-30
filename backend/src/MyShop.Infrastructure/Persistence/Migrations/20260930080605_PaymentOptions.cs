using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyShop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PaymentOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayLaterEnabled = table.Column<bool>(type: "bit", nullable: false),
                    OnlinePaymentEnabled = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentOptions", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "PaymentOptions",
                columns: new[] { "Id", "OnlinePaymentEnabled", "PayLaterEnabled", "Version" },
                values: new object[] { new Guid("5d484f50-9c6e-4e67-b5e0-3615cdb869eb"), false, true, new Guid("7732cb95-5f60-4f20-82f7-499a020a9d3e") });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentOptions");
        }
    }
}
