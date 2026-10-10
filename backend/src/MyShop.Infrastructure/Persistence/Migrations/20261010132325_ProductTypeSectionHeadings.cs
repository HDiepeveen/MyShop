using System;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace MyShop.Infrastructure.Persistence.Migrations
{
    public partial class ProductTypeSectionHeadings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Stop rather than silently choose between conflicting product-specific texts.
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT p.ProductTypeId FROM Products p JOIN ProductSeos s ON s.ProductId = p.Id
                    GROUP BY p.ProductTypeId
                    HAVING COUNT(DISTINCT s.AboutHeading COLLATE Latin1_General_100_BIN2) > 1
                        OR COUNT(DISTINCT s.AttributesHeading COLLATE Latin1_General_100_BIN2) > 1
                )
                    THROW 51000, 'Product section headings differ within a product type. Choose matching headings before migrating.', 1;
                """);
            migrationBuilder.AddColumn<string>(name: "AboutHeading", table: "ProductTypes", type: "nvarchar(200)", maxLength: 200, nullable: true);
            migrationBuilder.AddColumn<string>(name: "AttributesHeading", table: "ProductTypes", type: "nvarchar(200)", maxLength: 200, nullable: true);
            migrationBuilder.AddColumn<Guid>(name: "SectionHeadingsRevision", table: "ProductTypes", type: "uniqueidentifier", nullable: false, defaultValue: Guid.Empty);
            migrationBuilder.Sql("""
                UPDATE t SET AboutHeading = old.AboutHeading, AttributesHeading = old.AttributesHeading,
                    SectionHeadingsRevision = NEWID()
                FROM ProductTypes t JOIN (
                    SELECT p.ProductTypeId, MAX(s.AboutHeading) AboutHeading, MAX(s.AttributesHeading) AttributesHeading
                    FROM Products p JOIN ProductSeos s ON s.ProductId = p.Id GROUP BY p.ProductTypeId
                ) old ON old.ProductTypeId = t.Id
                WHERE old.AboutHeading IS NOT NULL OR old.AttributesHeading IS NOT NULL;
                """);
            migrationBuilder.DropColumn(name: "AboutHeading", table: "ProductSeos");
            migrationBuilder.DropColumn(name: "AttributesHeading", table: "ProductSeos");
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "AboutHeading", table: "ProductSeos", type: "nvarchar(200)", maxLength: 200, nullable: true);
            migrationBuilder.AddColumn<string>(name: "AttributesHeading", table: "ProductSeos", type: "nvarchar(200)", maxLength: 200, nullable: true);
            migrationBuilder.Sql("""
                INSERT INTO ProductSeos (ProductId, Version)
                SELECT p.Id, NEWID() FROM Products p JOIN ProductTypes t ON t.Id = p.ProductTypeId
                WHERE (t.AboutHeading IS NOT NULL OR t.AttributesHeading IS NOT NULL)
                    AND NOT EXISTS (SELECT 1 FROM ProductSeos s WHERE s.ProductId = p.Id);
                UPDATE s SET AboutHeading = t.AboutHeading, AttributesHeading = t.AttributesHeading
                FROM ProductSeos s JOIN Products p ON p.Id = s.ProductId JOIN ProductTypes t ON t.Id = p.ProductTypeId;
                """);
            migrationBuilder.DropColumn(name: "AboutHeading", table: "ProductTypes");
            migrationBuilder.DropColumn(name: "AttributesHeading", table: "ProductTypes");
            migrationBuilder.DropColumn(name: "SectionHeadingsRevision", table: "ProductTypes");
        }
    }
}
