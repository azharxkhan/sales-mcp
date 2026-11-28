using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesMcp.Infrastructure.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class SnapshotQuoteDocumentValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountNumber",
                table: "Quotes",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BusinessName",
                table: "Quotes",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "Quotes",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PaymentTermsBasis",
                table: "Quotes",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PaymentTermsDays",
                table: "Quotes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "QuoteLines",
                type: "TEXT",
                maxLength: 32,
                nullable: true);

            // Give existing quotes the values their documents showed until now: today's business, customer
            // and offering records. Every quote has a customer and every line an offering (foreign keys).
            migrationBuilder.Sql("""
                UPDATE "Quotes"
                SET "BusinessName" = b."Name",
                    "CustomerName" = c."Name",
                    "AccountNumber" = c."AccountNumber",
                    "PaymentTermsBasis" = c."PaymentTermsBasis",
                    "PaymentTermsDays" = c."PaymentTermsDays"
                FROM "Customers" AS c
                JOIN "Businesses" AS b ON b."Id" = c."BusinessId"
                WHERE c."Id" = "Quotes"."CustomerId";
                """);

            migrationBuilder.Sql("""
                UPDATE "QuoteLines"
                SET "Unit" = (SELECT o."Unit" FROM "Offerings" AS o WHERE o."Id" = "QuoteLines"."OfferingId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountNumber",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "BusinessName",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "PaymentTermsBasis",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "PaymentTermsDays",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "QuoteLines");
        }
    }
}
