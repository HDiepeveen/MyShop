using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyShop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RecordOrderPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PaidAt",
                table: "Orders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Version",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql("UPDATE [Orders] SET [Version] = NEWID() WHERE [Version] IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "Version",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Orders");
        }
    }
}
