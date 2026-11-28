using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesMcp.Infrastructure.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddOrderNumbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Number",
                table: "SalesOrders",
                type: "TEXT",
                maxLength: 14,
                nullable: false,
                defaultValue: "");

            // Number existing orders per year of OrderedOn, oldest first, so the index below can be unique.
            migrationBuilder.Sql("""
                UPDATE "SalesOrders"
                SET "Number" = (
                    SELECT numbered."Number"
                    FROM (
                        SELECT "Id",
                               'SO-' || substr("OrderedOn", 1, 4) || '-' || printf('%06d', ROW_NUMBER() OVER (
                                   PARTITION BY substr("OrderedOn", 1, 4) ORDER BY "OrderedOn", "Id")) AS "Number"
                        FROM "SalesOrders") AS numbered
                    WHERE numbered."Id" = "SalesOrders"."Id");
                """);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_Number",
                table: "SalesOrders",
                column: "Number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_Number",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "Number",
                table: "SalesOrders");
        }
    }
}
