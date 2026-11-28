using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesMcp.Infrastructure.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddOfferingCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Offerings",
                type: "TEXT",
                maxLength: 60,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Offerings");
        }
    }
}
