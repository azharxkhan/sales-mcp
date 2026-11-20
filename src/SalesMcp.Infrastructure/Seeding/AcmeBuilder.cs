using System.Globalization;
using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Customers;
using SalesMcp.Domain.Inventory;
using SalesMcp.Domain.Orders;
using SalesMcp.Domain.Pricing;
using SalesMcp.Domain.Quotes;
using SalesMcp.Domain.Tax;

namespace SalesMcp.Infrastructure.Seeding;

/// <summary>Generates one <see cref="SeedDataset"/>. Use once: it holds the random sequence.</summary>
internal sealed class AcmeBuilder(DateOnly today)
{
    private static readonly TimeSpan _southAfricanTime = TimeSpan.FromHours(2);
    private static readonly TimeOnly _quoteTime = new(9, 0);

    // Generated stock positions with nothing available. With 240 positions, 8 short is a 96.7% fill rate.
    private const int RandomShortages = 4;

    private readonly Random _random = new(AcmeSeed.RandomSeed);
    private readonly SeedManifest _manifest = AcmeSeed.Manifest;
    private readonly PriceResolver _resolver = new(VatRateTable.SouthAfrica);
    private readonly AgentId _salesDesk = AgentId.Create("acme-sales-desk").Value;

    private readonly List<Product> _products = [];
    private readonly Dictionary<OfferingId, decimal> _listPrices = [];
    private readonly Dictionary<OfferingId, string> _categories = [];

    public SeedDataset Build()
    {
        var business = Ok(Business.Create(_manifest.Business, AcmeCatalog.BusinessName, AcmeSeed.Currency, "en-ZA"));

        BuildCatalog();
        var rules = BuildRules();
        var warehouses = BuildWarehouses();
        var stock = BuildStock(warehouses);
        var priceLists = BuildPriceLists();
        var customers = BuildCustomers();
        var contacts = BuildContacts(customers);
        var contracts = BuildContracts(customers);
        var (quotes, orders) = BuildHistory(customers, priceLists, contracts);

        // Discontinued after the history, so old quotes still name them.
        foreach (var sku in AcmeCatalog.DiscontinuedSkus)
        {
            _products.Single(p => p.Sku.Value == sku).Deactivate();
        }

        return new SeedDataset(
            _manifest,
            business,
            _products,
            rules,
            warehouses,
            stock,
            priceLists,
            contracts,
            [.. customers.Select(c => c.Customer)],
            contacts,
            quotes,
            orders);
    }

    private void BuildCatalog()
    {
        foreach (var category in AcmeCatalog.Categories)
        {
            for (var item = 0; item < category.Items.Count; item++)
            {
                var spec = category.Items[item];
                for (var variant = 0; variant < spec.Variants.Count; variant++)
                {
                    var v = spec.Variants[variant];
                    var sku = string.Create(CultureInfo.InvariantCulture, $"ACM-{category.Code}-{item + 1}{variant + 1:D2}");
                    var name = string.Format(CultureInfo.InvariantCulture, spec.Template, v.Label);
                    var product = Ok(Product.Create(
                        AcmeSeed.OfferingIdFor(sku),
                        _manifest.Business,
                        sku,
                        name,
                        category.Name,
                        v.Vat ?? spec.Vat,
                        spec.Unit,
                        category.LeadTimeDays,
                        v.ReturnWindowDays ?? category.ReturnWindowDays));

                    _products.Add(product);
                    _listPrices.Add(product.Id, v.ListPrice);
                    _categories.Add(product.Id, category.Code);
                }
            }
        }
    }

    private static List<OfferingRule> BuildRules()
    {
        // Adding to a rule set checks for duplicates and conflicts, as the tools will.
        var set = new OfferingRuleSet();
        var rules = new List<OfferingRule>();
        foreach (var spec in AcmeCatalog.Rules)
        {
            var rule = Ok(OfferingRule.Create(
                AcmeSeed.OfferingIdFor(spec.FromSku), spec.Kind, AcmeSeed.OfferingIdFor(spec.ToSku), spec.Reason));
            Ok(set.Add(rule));
            rules.Add(rule);
        }

        return rules;
    }

    private List<Warehouse> BuildWarehouses() =>
    [
        Ok(Warehouse.Create(_manifest.Warehouses.Johannesburg, _manifest.Business, "JHB", "Johannesburg distribution centre")),
        Ok(Warehouse.Create(_manifest.Warehouses.Durban, _manifest.Business, "DBN", "Durban distribution centre")),
    ];

    private List<StockLevel> BuildStock(List<Warehouse> warehouses)
    {
        var positions = _products.SelectMany(p => warehouses.Select(w => (Product: p, Warehouse: w))).ToList();

        // Shortages: the fixtures, plus a few random positions (half with stock on order).
        var shortWithReceipt = new HashSet<(OfferingId, WarehouseId)>();
        var shortWithoutReceipt = new HashSet<(OfferingId, WarehouseId)>();
        foreach (var w in warehouses)
        {
            shortWithReceipt.Add((_manifest.Offerings.ShortWithReceipt, w.Id));
            shortWithoutReceipt.Add((_manifest.Offerings.ShortWithoutReceipt, w.Id));
        }

        var candidates = positions.Where(p => !AcmeCatalog.FixtureSkus.Contains(p.Product.Sku.Value)).ToList();
        for (var i = 0; i < RandomShortages; i++)
        {
            var pick = candidates[_random.Next(candidates.Count)];
            candidates.Remove(pick);
            (i % 2 == 0 ? shortWithReceipt : shortWithoutReceipt).Add((pick.Product.Id, pick.Warehouse.Id));
        }

        var levels = new List<StockLevel>();
        foreach (var (product, warehouse) in positions)
        {
            var typical = Math.Clamp(decimal.Round(4000m / _listPrices[product.Id]), 2m, 400m);
            var share = warehouse.Id == _manifest.Warehouses.Johannesburg ? 1m : 0.6m;
            var key = (product.Id, warehouse.Id);

            decimal onHand, allocated, onOrder = 0m;
            DateOnly? receipt = null;
            if (shortWithReceipt.Contains(key) || shortWithoutReceipt.Contains(key))
            {
                // Whatever is on the shelf is already promised to other orders.
                onHand = _random.Next(0, 4);
                allocated = onHand;
                if (shortWithReceipt.Contains(key))
                {
                    onOrder = typical;
                    receipt = today.AddDays(_random.Next(5, 22));
                }
            }
            else
            {
                onHand = Math.Max(1m, decimal.Round(typical * share * (0.5m + (decimal)_random.NextDouble())));
                allocated = decimal.Floor(onHand * (decimal)_random.NextDouble() * 0.2m);
                if (_random.Next(5) == 0)
                {
                    onOrder = Math.Max(1m, decimal.Round(typical * (0.5m + ((decimal)_random.NextDouble() * 0.5m))));
                    receipt = today.AddDays(_random.Next(3, 29));
                }
            }

            levels.Add(Ok(StockLevel.Create(product.Id, warehouse.Id, onHand, allocated, onOrder, receipt)));
        }

        return levels;
    }

    private List<PriceList> BuildPriceLists()
    {
        var standard = Ok(PriceList.Create(_manifest.PriceLists.Default, _manifest.Business, "Acme list prices", AcmeSeed.Currency, isDefault: true));
        var mining = Ok(PriceList.Create(_manifest.PriceLists.Mining, _manifest.Business, "Mining accounts", AcmeSeed.Currency));
        var trade = Ok(PriceList.Create(_manifest.PriceLists.Trade, _manifest.Business, "Trade partners", AcmeSeed.Currency));

        foreach (var product in _products)
        {
            var list = _listPrices[product.Id];
            var category = _categories[product.Id];
            Ok(standard.AddItem(product.Id, new Quantity(1m), Zar(list)));

            if (AcmeCatalog.QuantityBreakCategories.Contains(category) && list < 500m && product.VatCode != VatCode.Exempt)
            {
                Ok(standard.AddItem(product.Id, new Quantity(10m), Zar(Cents(list * 0.95m))));
                Ok(standard.AddItem(product.Id, new Quantity(50m), Zar(Cents(list * 0.90m))));
            }

            if (AcmeCatalog.MiningCategories.Contains(category))
            {
                Ok(mining.AddItem(product.Id, new Quantity(1m), Zar(Cents(list * 0.92m))));
            }

            if (AcmeCatalog.TradeCategories.Contains(category))
            {
                Ok(trade.AddItem(product.Id, new Quantity(1m), Zar(Cents(list * 0.95m))));
            }
        }

        return [standard, mining, trade];
    }

    private List<SeededCustomer> BuildCustomers()
    {
        var customers = new List<SeededCustomer>();
        for (var rank = 1; rank <= AcmeCatalog.CustomerNames.Count; rank++)
        {
            // Pareto: size falls off with rank, so a handful of customers carry most of the revenue.
            var weight = 1.0 / Math.Pow(rank, 1.15);
            var limit = Math.Max(20_000m, decimal.Round(600_000m * (decimal)weight / 5_000m) * 5_000m);

            var draw = (decimal)_random.NextDouble();
            var balance = rank == AcmeCatalog.OnHoldRank ? limit * 1.08m
                : AcmeCatalog.NearCreditLimitRanks.Contains(rank) ? limit * (0.92m + (draw * 0.05m))
                : limit * (0.15m + (draw * 0.45m));

            var terms = AcmeCatalog.CashRanks.Contains(rank) ? PaymentTerms.CashWithOrder
                : rank <= 10 ? Ok(PaymentTerms.Create(PaymentTermsBasis.DaysFromEndOfMonth, 30))
                : rank >= 31 ? Ok(PaymentTerms.Create(PaymentTermsBasis.DaysFromInvoice, 7))
                : Ok(PaymentTerms.Create(PaymentTermsBasis.DaysFromInvoice, 30));

            PriceListId? priceList = AcmeCatalog.MiningRanks.Contains(rank) ? _manifest.PriceLists.Mining
                : AcmeCatalog.TradeRanks.Contains(rank) ? _manifest.PriceLists.Trade
                : null;

            var vatNumber = rank >= AcmeCatalog.FirstUnregisteredRank
                ? null
                : string.Create(CultureInfo.InvariantCulture, $"4{_random.Next(100_000_000, 1_000_000_000)}");

            var customer = Ok(Customer.Create(
                AcmeSeed.CustomerIdFor(rank),
                _manifest.Business,
                AcmeCatalog.AccountNumberFor(rank),
                AcmeCatalog.CustomerNames[rank - 1],
                vatNumber,
                Zar(limit),
                Zar(Cents(balance)),
                terms,
                priceList,
                allowBackOrders: !AcmeCatalog.NoBackOrderRanks.Contains(rank),
                allowPartialShipment: !AcmeCatalog.NoPartialShipmentRanks.Contains(rank)));

            if (rank == AcmeCatalog.OnHoldRank)
            {
                customer.PlaceOnHold();
            }

            customers.Add(new SeededCustomer(rank, weight, customer));
        }

        return customers;
    }

    private List<Contact> BuildContacts(List<SeededCustomer> customers)
    {
        var sources = Enum.GetValues<ConsentSource>();
        var contacts = new List<Contact>();
        foreach (var (rank, _, customer) in customers)
        {
            var count = rank <= 10 ? 3 : rank <= 25 ? 2 : 1;
            var domain = new string([.. customer.Name.Split(' ').Take(2).SelectMany(w => w).Where(char.IsAsciiLetter)]) + ".example";
            var used = new HashSet<string>(StringComparer.Ordinal);

            for (var n = 1; n <= count; n++)
            {
                string first, last;
                do
                {
                    first = AcmeCatalog.FirstNames[_random.Next(AcmeCatalog.FirstNames.Count)];
                    last = AcmeCatalog.LastNames[_random.Next(AcmeCatalog.LastNames.Count)];
                }
                while (!used.Add(first + last));

                var email = first + "." + last.Replace(" ", string.Empty, StringComparison.Ordinal) + "@" + domain;
                var contact = Ok(Contact.Create(
                    new ContactId(SeedIds.For($"acme/contact/{customer.AccountNumber}/{n}")),
                    customer.Id,
                    first + " " + last,
                    email));

                // Half opted in, a fifth opted out, the rest never asked: marketing tools must check.
                var roll = _random.Next(10);
                var recordedAt = new DateTimeOffset(today.AddDays(-_random.Next(30, 400)).ToDateTime(_quoteTime), _southAfricanTime);
                var source = sources[_random.Next(sources.Length)];
                if (roll < 5)
                {
                    contact.RecordMarketingConsent(Ok(MarketingConsent.OptIn(recordedAt, source)));
                }
                else if (roll < 7)
                {
                    contact.RecordMarketingConsent(Ok(MarketingConsent.OptOut(recordedAt, source)));
                }

                contacts.Add(contact);
            }
        }

        return contacts;
    }

    private List<ContractPrice> BuildContracts(List<SeededCustomer> customers)
    {
        var contracts = new List<ContractPrice>();
        var eligible = _products.Where(p => p.VatCode != VatCode.Exempt).ToList();
        foreach (var (rank, _, customer) in customers.Where(c => AcmeCatalog.ContractRanks.Contains(c.Rank)))
        {
            var chosen = new HashSet<OfferingId>();
            while (chosen.Count < 6)
            {
                var product = eligible[_random.Next(eligible.Count)];
                if (!chosen.Add(product.Id))
                {
                    continue;
                }

                var rate = 0.82m + (_random.Next(0, 7) * 0.01m);
                contracts.Add(Ok(ContractPrice.Create(
                    customer.Id,
                    product.Id,
                    Zar(Cents(_listPrices[product.Id] * rate)),
                    today.AddDays(-200),
                    today.AddDays(165))));
            }

            // The top customer always has a contract on the drill, so tests can rely on one.
            if (rank == AcmeCatalog.TopRank && !chosen.Contains(_manifest.Offerings.DrillBody))
            {
                contracts.Add(Ok(ContractPrice.Create(
                    customer.Id,
                    _manifest.Offerings.DrillBody,
                    Zar(Cents(_listPrices[_manifest.Offerings.DrillBody] * 0.85m)),
                    today.AddDays(-200),
                    today.AddDays(165))));
            }
        }

        return contracts;
    }

    private (List<Quote> Quotes, List<SalesOrder> Orders) BuildHistory(
        List<SeededCustomer> customers,
        List<PriceList> priceLists,
        List<ContractPrice> contracts)
    {
        var plan = new List<PlannedQuote>();
        foreach (var seeded in customers)
        {
            // Established customers ordered before the window opened, so their window quotes count as existing business.
            if (seeded.Rank < AcmeCatalog.FirstNewRank)
            {
                plan.Add(new PlannedQuote(today.AddDays(-(AcmeSeed.HistoryDays + 5 + _random.Next(0, 20))), seeded, Opening: true));
            }

            var count = Math.Max(5, (int)Math.Round(45 / Math.Pow(seeded.Rank, 0.75)));
            for (var i = 0; i < count; i++)
            {
                plan.Add(new PlannedQuote(today.AddDays(-_random.Next(1, AcmeSeed.HistoryDays + 1)), seeded, Opening: false));
            }
        }

        var standard = priceLists.Single(l => l.IsDefault);
        var quotes = new List<Quote>();
        var orders = new List<SalesOrder>();
        var orderDates = customers.ToDictionary(c => c.Customer.Id, _ => new List<DateOnly>());
        var sequences = new Dictionary<int, int>();
        var outcomes = new Dictionary<CustomerSegment, WinTracker>
        {
            [CustomerSegment.New] = new(AcmeSeed.NewCustomerWinRate),
            [CustomerSegment.Existing] = new(AcmeSeed.ExistingCustomerWinRate),
        };

        foreach (var planned in plan.OrderBy(p => p.IssuedOn).ThenBy(p => p.Customer.Rank))
        {
            var (issuedOn, seeded, opening) = planned;
            var customer = seeded.Customer;

            var year = issuedOn.Year;
            sequences[year] = sequences.GetValueOrDefault(year) + 1;
            var number = Ok(QuoteNumber.From(year, sequences[year]));

            var sources = new PriceSources(
                standard,
                priceLists.SingleOrDefault(l => l.Id == customer.PriceListId),
                [.. contracts.Where(c => c.CustomerId == customer.Id)]);

            var quote = Ok(Quote.Create(
                new QuoteId(SeedIds.For("acme/quote/" + number.Value)),
                number,
                customer,
                issuedOn >= today.AddDays(-90) ? _manifest.Agent : _salesDesk,
                issuedOn,
                PriceLines(seeded, issuedOn, sources),
                RiskReducers()));
            quotes.Add(quote);

            bool won;
            if (opening)
            {
                won = true;
            }
            else if (issuedOn > today.AddDays(-AcmeSeed.OpenQuoteDays))
            {
                continue; // still open
            }
            else
            {
                var segment = CustomerSegments.From(orderDates[customer.Id].Count(d => d < issuedOn));
                won = outcomes[segment].Decide(_random, forceLoss: seeded.Rank == AcmeCatalog.NewRank);
            }

            if (!won)
            {
                // Most lost quotes are declined; the rest simply lapse.
                if (_random.Next(100) < 65)
                {
                    Ok(quote.Decline());
                }

                continue;
            }

            var orderedOn = issuedOn.AddDays(opening ? 5 : _random.Next(1, AcmeSeed.OpenQuoteDays + 1));
            var order = Ok(SalesOrder.FromQuote(new OrderId(SeedIds.For("acme/order/" + number.Value)), quote, orderedOn));
            Advance(order, today.DayNumber - orderedOn.DayNumber);
            orders.Add(order);
            orderDates[customer.Id].Add(orderedOn);
        }

        return (quotes, orders);
    }

    private List<PricedOffering> PriceLines(SeededCustomer seeded, DateOnly issuedOn, PriceSources sources)
    {
        var asOf = new DateTimeOffset(issuedOn.ToDateTime(_quoteTime), _southAfricanTime);
        var scale = 2.0 + (24.0 * seeded.Weight);
        var lineCount = 1 + _random.Next(4);
        var chosen = new HashSet<OfferingId>();
        var lines = new List<PricedOffering>();

        while (lines.Count < lineCount)
        {
            var product = _products[_random.Next(_products.Count)];
            if (!chosen.Add(product.Id))
            {
                continue;
            }

            // Bigger customers buy more; cheaper items sell in bigger quantities.
            var priceFactor = Math.Clamp(500.0 / (double)_listPrices[product.Id], 0.2, 5.0);
            var quantity = Math.Max(1, (int)Math.Round(_random.Next(1, 7) * scale * priceFactor));
            var discount = _random.Next(10) switch
            {
                < 7 => 0m,
                < 9 => 2.5m,
                _ => 5m,
            };

            var price = Ok(_resolver.Resolve(
                new PriceRequest(product, seeded.Customer, new Quantity(quantity), new Percentage(discount), asOf),
                sources));
            lines.Add(new PricedOffering(product, price));
        }

        return lines;
    }

    private List<RiskReducer> RiskReducers() => _random.Next(10) switch
    {
        < 2 => [RiskReducer.ReturnWindow],
        2 => [RiskReducer.PhasedDelivery],
        _ => [],
    };

    /// <summary>Orders over a month old are invoiced; newer ones are still shipping or awaiting dispatch.</summary>
    private static void Advance(SalesOrder order, int ageDays)
    {
        Ok(order.Release());
        if (ageDays >= 14)
        {
            Ok(order.Ship());
        }

        if (ageDays > 30)
        {
            Ok(order.Invoice());
        }
    }

    private static Money Zar(decimal amount) => new(amount, AcmeSeed.Currency);

    private static decimal Cents(decimal amount) => decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

    private static T Ok<T>(Result<T> result) =>
        result.IsSuccess ? result.Value : throw Invalid(result.Error);

    private static void Ok(Result result)
    {
        if (result.IsFailure)
        {
            throw Invalid(result.Error);
        }
    }

    private static InvalidOperationException Invalid(DomainError error) =>
        new($"The Acme seed data broke a domain rule: {error.Code}: {error.Message}");

    private sealed record SeededCustomer(int Rank, double Weight, Customer Customer);

    private sealed record PlannedQuote(DateOnly IssuedOn, SeededCustomer Customer, bool Opening);

    /// <summary>
    /// Keeps a segment's win rate on target. Pure coin flips would wander several points off with
    /// only a few hundred quotes; this wins when the running rate falls behind, with some jitter.
    /// </summary>
    private sealed class WinTracker(double target)
    {
        private int _decided;
        private int _won;

        public bool Decide(Random random, bool forceLoss)
        {
            var jitter = (random.NextDouble() - 0.5) * 0.8;
            var win = !forceLoss && _won + 1 <= (target * (_decided + 1)) + jitter;
            _decided++;
            _won += win ? 1 : 0;
            return win;
        }
    }
}
