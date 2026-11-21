using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HDWebFramework.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OverheidIoVehicleSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OverheidIoSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Revision = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ColumnsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProtectedApiKey = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OverheidIoSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "OverheidIoSettings",
                columns: new[] { "Id", "ColumnsJson", "ProtectedApiKey", "Revision" },
                values: new object[] { 1, "[\"kenteken\",\"merk\",\"handelsbenaming\",\"voertuigsoort\",\"eerste_kleur\",\"datum_eerste_toelating\",\"vervaldatum_apk\",\"aantal_zitplaatsen\",\"aantal_deuren\",\"massa_ledig_voertuig\",\"cilinderinhoud\",\"aantal_cilinders\",\"tellerstandoordeel\",\"brandstof.brandstof_omschrijving\"]", null, new Guid("e74b8e60-9f7e-4e08-9a88-70b6777ac215") });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OverheidIoSettings");
        }
    }
}
