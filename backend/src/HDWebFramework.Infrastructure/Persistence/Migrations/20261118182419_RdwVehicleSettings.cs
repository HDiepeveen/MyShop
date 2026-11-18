using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HDWebFramework.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RdwVehicleSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RdwSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Revision = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ColumnsJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RdwSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "RdwSettings",
                columns: new[] { "Id", "ColumnsJson", "Revision" },
                values: new object[] { 1, "[\"kenteken\",\"merk\",\"handelsbenaming\",\"voertuigsoort\",\"eerste_kleur\",\"datum_eerste_toelating\",\"vervaldatum_apk\",\"aantal_zitplaatsen\",\"aantal_deuren\",\"massa_ledig_voertuig\",\"cilinderinhoud\",\"aantal_cilinders\",\"tellerstandoordeel\"]", new Guid("790213d2-9aa5-4f0b-8de7-906eb8eb95b2") });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RdwSettings");
        }
    }
}
