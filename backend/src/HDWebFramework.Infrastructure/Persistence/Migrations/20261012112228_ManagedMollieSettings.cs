using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HDWebFramework.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ManagedMollieSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MollieManaged",
                table: "PaymentOptions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MollieReturnUrl",
                table: "PaymentOptions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MollieRevision",
                table: "PaymentOptions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "MollieWebhookUrl",
                table: "PaymentOptions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProtectedMollieKey",
                table: "PaymentOptions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "PaymentOptions",
                keyColumn: "Id",
                keyValue: new Guid("5d484f50-9c6e-4e67-b5e0-3615cdb869eb"),
                columns: new[] { "MollieManaged", "MollieReturnUrl", "MollieRevision", "MollieWebhookUrl", "ProtectedMollieKey" },
                values: new object[] { false, null, new Guid("00000000-0000-0000-0000-000000000000"), null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MollieManaged",
                table: "PaymentOptions");

            migrationBuilder.DropColumn(
                name: "MollieReturnUrl",
                table: "PaymentOptions");

            migrationBuilder.DropColumn(
                name: "MollieRevision",
                table: "PaymentOptions");

            migrationBuilder.DropColumn(
                name: "MollieWebhookUrl",
                table: "PaymentOptions");

            migrationBuilder.DropColumn(
                name: "ProtectedMollieKey",
                table: "PaymentOptions");
        }
    }
}
