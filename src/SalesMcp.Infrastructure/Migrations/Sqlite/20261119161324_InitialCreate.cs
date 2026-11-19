using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesMcp.Infrastructure.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Businesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Locale = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Businesses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Offerings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BusinessId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Sku = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    VatCode = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    BillingPeriod = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    LeadTimeDays = table.Column<int>(type: "INTEGER", nullable: true),
                    ReturnWindowDays = table.Column<int>(type: "INTEGER", nullable: true),
                    PricingModel = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offerings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Offerings_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PriceLists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BusinessId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceLists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceLists_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Warehouses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BusinessId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Warehouses_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OfferingRules",
                columns: table => new
                {
                    OfferingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TargetOfferingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfferingRules", x => new { x.OfferingId, x.TargetOfferingId });
                    table.ForeignKey(
                        name: "FK_OfferingRules_Offerings_OfferingId",
                        column: x => x.OfferingId,
                        principalTable: "Offerings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OfferingRules_Offerings_TargetOfferingId",
                        column: x => x.TargetOfferingId,
                        principalTable: "Offerings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BusinessId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    VatNumber = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    PriceListId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AllowBackOrders = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowPartialShipment = table.Column<bool>(type: "INTEGER", nullable: false),
                    OnHold = table.Column<bool>(type: "INTEGER", nullable: false),
                    Balance_Amount = table.Column<long>(type: "INTEGER", precision: 19, scale: 4, nullable: false),
                    Balance_Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    CreditLimit_Amount = table.Column<long>(type: "INTEGER", precision: 19, scale: 4, nullable: false),
                    CreditLimit_Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    PaymentTermsBasis = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    PaymentTermsDays = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customers_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Customers_PriceLists_PriceListId",
                        column: x => x.PriceListId,
                        principalTable: "PriceLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PriceListItems",
                columns: table => new
                {
                    OfferingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MinQuantity = table.Column<long>(type: "INTEGER", nullable: false),
                    PriceListId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UnitPrice_Amount = table.Column<long>(type: "INTEGER", precision: 19, scale: 4, nullable: false),
                    UnitPrice_Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceListItems", x => new { x.PriceListId, x.OfferingId, x.MinQuantity });
                    table.ForeignKey(
                        name: "FK_PriceListItems_Offerings_OfferingId",
                        column: x => x.OfferingId,
                        principalTable: "Offerings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PriceListItems_PriceLists_PriceListId",
                        column: x => x.PriceListId,
                        principalTable: "PriceLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockLevels",
                columns: table => new
                {
                    OfferingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OnHand = table.Column<long>(type: "INTEGER", precision: 18, scale: 4, nullable: false),
                    Allocated = table.Column<long>(type: "INTEGER", precision: 18, scale: 4, nullable: false),
                    OnOrder = table.Column<long>(type: "INTEGER", precision: 18, scale: 4, nullable: false),
                    NextReceiptDate = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockLevels", x => new { x.OfferingId, x.WarehouseId });
                    table.ForeignKey(
                        name: "FK_StockLevels_Offerings_OfferingId",
                        column: x => x.OfferingId,
                        principalTable: "Offerings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockLevels_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Contacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: true),
                    MarketingOptedIn = table.Column<bool>(type: "INTEGER", nullable: true),
                    MarketingRecordedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    MarketingSource = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contacts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContractPrices",
                columns: table => new
                {
                    CustomerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OfferingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ValidTo = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    UnitPrice_Amount = table.Column<long>(type: "INTEGER", precision: 19, scale: 4, nullable: false),
                    UnitPrice_Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractPrices", x => new { x.CustomerId, x.OfferingId, x.ValidFrom });
                    table.ForeignKey(
                        name: "FK_ContractPrices_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContractPrices_Offerings_OfferingId",
                        column: x => x.OfferingId,
                        principalTable: "Offerings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Quotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Number = table.Column<string>(type: "TEXT", maxLength: 13, nullable: false),
                    CustomerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AgentId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    IssuedOn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ValidUntil = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    RiskReducers = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Quotes_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuoteLines",
                columns: table => new
                {
                    LineNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    QuoteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OfferingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Sku = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PriceSnapshot = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteLines", x => new { x.QuoteId, x.LineNumber });
                    table.ForeignKey(
                        name: "FK_QuoteLines_Offerings_OfferingId",
                        column: x => x.OfferingId,
                        principalTable: "Offerings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuoteLines_Quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "Quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AgentId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SourceQuoteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrderedOn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOrders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrders_Quotes_SourceQuoteId",
                        column: x => x.SourceQuoteId,
                        principalTable: "Quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderLines",
                columns: table => new
                {
                    LineNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    OrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OfferingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Sku = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Quantity = table.Column<long>(type: "INTEGER", nullable: false),
                    VatSnapshot = table.Column<string>(type: "TEXT", nullable: false),
                    UnitPrice_Amount = table.Column<long>(type: "INTEGER", precision: 19, scale: 4, nullable: false),
                    UnitPrice_Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderLines", x => new { x.OrderId, x.LineNumber });
                    table.ForeignKey(
                        name: "FK_OrderLines_Offerings_OfferingId",
                        column: x => x.OfferingId,
                        principalTable: "Offerings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderLines_SalesOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_CustomerId",
                table: "Contacts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractPrices_OfferingId",
                table: "ContractPrices",
                column: "OfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_BusinessId_AccountNumber",
                table: "Customers",
                columns: new[] { "BusinessId", "AccountNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_PriceListId",
                table: "Customers",
                column: "PriceListId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferingRules_TargetOfferingId",
                table: "OfferingRules",
                column: "TargetOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_Offerings_BusinessId_Sku",
                table: "Offerings",
                columns: new[] { "BusinessId", "Sku" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_OfferingId",
                table: "OrderLines",
                column: "OfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItems_OfferingId",
                table: "PriceListItems",
                column: "OfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceLists_DefaultPerBusiness",
                table: "PriceLists",
                column: "BusinessId",
                unique: true,
                filter: "\"IsDefault\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteLines_OfferingId",
                table: "QuoteLines",
                column: "OfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_CustomerId",
                table: "Quotes",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_Number",
                table: "Quotes",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CustomerId",
                table: "SalesOrders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_SourceQuoteId",
                table: "SalesOrders",
                column: "SourceQuoteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockLevels_WarehouseId",
                table: "StockLevels",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_BusinessId_Code",
                table: "Warehouses",
                columns: new[] { "BusinessId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Contacts");

            migrationBuilder.DropTable(
                name: "ContractPrices");

            migrationBuilder.DropTable(
                name: "OfferingRules");

            migrationBuilder.DropTable(
                name: "OrderLines");

            migrationBuilder.DropTable(
                name: "PriceListItems");

            migrationBuilder.DropTable(
                name: "QuoteLines");

            migrationBuilder.DropTable(
                name: "StockLevels");

            migrationBuilder.DropTable(
                name: "SalesOrders");

            migrationBuilder.DropTable(
                name: "Offerings");

            migrationBuilder.DropTable(
                name: "Warehouses");

            migrationBuilder.DropTable(
                name: "Quotes");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "PriceLists");

            migrationBuilder.DropTable(
                name: "Businesses");
        }
    }
}
