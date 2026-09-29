using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyShop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParentCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Categories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AttributeDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataType = table.Column<int>(type: "int", nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsFilterable = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttributeDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttributeDefinitions_ProductTypes_ProductTypeId",
                        column: x => x.ProductTypeId,
                        principalTable: "ProductTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_ProductTypes_ProductTypeId",
                        column: x => x.ProductTypeId,
                        principalTable: "ProductTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductAttributeValues",
                columns: table => new
                {
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttributeDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataType = table.Column<int>(type: "int", nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    TextValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IntegerValue = table.Column<long>(type: "bigint", nullable: true),
                    DecimalCoefficient = table.Column<decimal>(type: "decimal(29,0)", precision: 29, scale: 0, nullable: true),
                    DecimalScale = table.Column<byte>(type: "tinyint", nullable: true),
                    BooleanValue = table.Column<bool>(type: "bit", nullable: true),
                    DateValue = table.Column<DateOnly>(type: "date", nullable: true),
                    ChoiceValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAttributeValues", x => new { x.ProductId, x.AttributeDefinitionId });
                    table.ForeignKey(
                        name: "FK_ProductAttributeValues_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductCategories",
                columns: table => new
                {
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCategories", x => new { x.ProductId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_ProductCategories_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sku = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, collation: "Latin1_General_100_BIN2"),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    PriceAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PriceCurrency = table.Column<string>(type: "char(3)", maxLength: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariants_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductAttributeMultiChoiceValues",
                columns: table => new
                {
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttributeDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAttributeMultiChoiceValues", x => new { x.ProductId, x.AttributeDefinitionId, x.Ordinal });
                    table.ForeignKey(
                        name: "FK_ProductAttributeMultiChoiceValues_ProductAttributeValues_ProductId_AttributeDefinitionId",
                        columns: x => new { x.ProductId, x.AttributeDefinitionId },
                        principalTable: "ProductAttributeValues",
                        principalColumns: new[] { "ProductId", "AttributeDefinitionId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PriceRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AdjustmentType = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EndsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceRules_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariantAttributeValues",
                columns: table => new
                {
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttributeDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataType = table.Column<int>(type: "int", nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    TextValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IntegerValue = table.Column<long>(type: "bigint", nullable: true),
                    DecimalCoefficient = table.Column<decimal>(type: "decimal(29,0)", precision: 29, scale: 0, nullable: true),
                    DecimalScale = table.Column<byte>(type: "tinyint", nullable: true),
                    BooleanValue = table.Column<bool>(type: "bit", nullable: true),
                    DateValue = table.Column<DateOnly>(type: "date", nullable: true),
                    ChoiceValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariantAttributeValues", x => new { x.ProductVariantId, x.AttributeDefinitionId });
                    table.ForeignKey(
                        name: "FK_ProductVariantAttributeValues_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariantAttributeMultiChoiceValues",
                columns: table => new
                {
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttributeDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariantAttributeMultiChoiceValues", x => new { x.ProductVariantId, x.AttributeDefinitionId, x.Ordinal });
                    table.ForeignKey(
                        name: "FK_ProductVariantAttributeMultiChoiceValues_ProductVariantAttributeValues_ProductVariantId_AttributeDefinitionId",
                        columns: x => new { x.ProductVariantId, x.AttributeDefinitionId },
                        principalTable: "ProductVariantAttributeValues",
                        principalColumns: new[] { "ProductVariantId", "AttributeDefinitionId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttributeDefinitions_ProductTypeId_Code",
                table: "AttributeDefinitions",
                columns: new[] { "ProductTypeId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentCategoryId",
                table: "Categories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceRules_ProductVariantId_Priority",
                table: "PriceRules",
                columns: new[] { "ProductVariantId", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductTypeId",
                table: "Products",
                column: "ProductTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId",
                table: "ProductVariants",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "UX_ProductVariants_Sku",
                table: "ProductVariants",
                column: "Sku",
                unique: true,
                filter: "[Sku] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttributeDefinitions");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "PriceRules");

            migrationBuilder.DropTable(
                name: "ProductAttributeMultiChoiceValues");

            migrationBuilder.DropTable(
                name: "ProductCategories");

            migrationBuilder.DropTable(
                name: "ProductVariantAttributeMultiChoiceValues");

            migrationBuilder.DropTable(
                name: "ProductAttributeValues");

            migrationBuilder.DropTable(
                name: "ProductVariantAttributeValues");

            migrationBuilder.DropTable(
                name: "ProductVariants");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "ProductTypes");
        }
    }
}
