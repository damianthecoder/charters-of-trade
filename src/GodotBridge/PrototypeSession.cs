using ChartersOfTrade.AI.Company;
using ChartersOfTrade.CitySim.Core;
using ChartersOfTrade.Content.Core;
using ChartersOfTrade.Economy.Core;
using ChartersOfTrade.Logistics.Core;
using ChartersOfTrade.Persistence.Core;
using ChartersOfTrade.WorldGen.Core;

namespace ChartersOfTrade.GodotBridge;

public sealed record PrototypeCityView(
    string Id,
    string Name,
    int X,
    int Y,
    CityLevel Level,
    int Population,
    double SupplySatisfaction,
    IReadOnlyDictionary<string, int> MarketStock,
    IReadOnlyDictionary<string, int> CompanyWarehouse,
    IReadOnlyList<PrototypeMarketSignal> MarketSignals);

public sealed record PrototypeMarketSignal(
    string ResourceId,
    decimal Price,
    double Scarcity,
    int MarketStock,
    int WarehouseStock,
    int DesiredStock,
    int ConsumptionPerTick,
    int SafetyStock,
    int ReorderPoint,
    int ShipmentPriority,
    string Reason,
    string PolicyAction);

public sealed record PrototypeLedgerEntry(
    int Tick,
    string Category,
    string Message,
    decimal CashDelta,
    string RelatedId);

public sealed record PrototypeRouteContractView(
    string Id,
    string RouteId,
    string FromNode,
    string ToNode,
    string ResourceId,
    decimal ExpectedRevenue,
    decimal TransportCost,
    decimal ExpectedNet,
    int CapacityPerDay,
    int Units,
    int ShipmentPriority,
    string PolicyAction);

public sealed record PrototypeSnapshot(
    int Tick,
    GeneratedWorld World,
    string ContentHash,
    IReadOnlyList<ResourceDef> Resources,
    IReadOnlyList<TradeRoute> Routes,
    IReadOnlyList<MarketPrice> Prices,
    IReadOnlyList<PrototypeCityView> Cities,
    CompanyState Company,
    CalendarState Calendar,
    OpportunityScore AiChoice,
    IReadOnlyList<PrototypeLedgerEntry> Ledger,
    string SaveHash)
{
    public IReadOnlyList<PrototypeRouteContractView> AvailableContracts { get; init; } = [];

    public string? SelectedContractId { get; init; }

    public double UnmetDemandRatio => Prices.Count == 0 ? 0 : Math.Round(Prices.Average(price => Math.Max(0, price.Scarcity)), 4);

    public decimal LastTickCashDelta => Ledger.Where(entry => entry.Tick == Tick).Sum(entry => entry.CashDelta);
}

public sealed class PrototypeSession
{
    private readonly GameContent _content;
    private readonly IReadOnlyList<MarketNeed> _needs;
    private readonly EconomyTick _economy = new();
    private readonly CityGrowthSystem _cityGrowth = new();
    private readonly List<RuntimeCity> _cities;
    private readonly List<PrototypeLedgerEntry> _ledger = [];

    private CompanyState _company = new(1000m, 0m, 50, "merchant_league");
    private CalendarState _calendar = new(1, 1);
    private OpportunityScore _aiChoice = new("none", 0m);
    private string? _selectedContractId;

    public PrototypeSession(GeneratedWorld world, GameContent content, IReadOnlyList<TradeRoute> routes)
    {
        World = world;
        _content = content;
        Routes = routes;
        _needs = StarterScenarioFactory.CreateNeeds(content.Resources);
        _cities = world.Nodes.OrderBy(node => node.Id, StringComparer.Ordinal).Select(CreateCity).ToList();
        Current = BuildSnapshot(0);
    }

    public GeneratedWorld World { get; }

    public IReadOnlyList<TradeRoute> Routes { get; }

    public PrototypeSnapshot Current { get; private set; }

    public bool SelectRouteContract(string contractId)
    {
        if (string.IsNullOrWhiteSpace(contractId))
        {
            return false;
        }

        var contracts = BuildAvailableContracts();
        if (contracts.All(contract => contract.Id != contractId))
        {
            return false;
        }

        _selectedContractId = contractId;
        Current = BuildSnapshot(Current.Tick);
        return true;
    }

    public PrototypeSnapshot AdvanceTick()
    {
        var nextTick = Current.Tick + 1;
        _calendar = _calendar with { DayOfYear = _calendar.DayOfYear + 1 };
        var selectedContract = SelectedAvailableContract();
        var reservation = ReservationFor(selectedContract);

        var productionCash = RunProduction(nextTick, reservation);
        var logisticsCash = RunLogistics(nextTick);
        var aiCash = RunAi(nextTick);
        RunCityGrowth(nextTick);

        var cashDelta = productionCash + logisticsCash + aiCash;
        _company = _company with { Cash = decimal.Round(_company.Cash + cashDelta, 2, MidpointRounding.AwayFromZero) };

        Current = BuildSnapshot(nextTick);
        return Current;
    }

    private RuntimeCity CreateCity(WorldNode node, int index)
    {
        var market = StarterScenarioFactory.CreateInitialMarket(_content.Resources);
        var warehouse = new Inventory();

        foreach (var resourceId in node.Resources.Order(StringComparer.Ordinal))
        {
            warehouse.Add(resourceId, 8);
            market.Add(resourceId, 4);
        }

        var population = index switch
        {
            0 => new PopulationCohorts(90, 12, 4, 0),
            <= 2 => new PopulationCohorts(70, 8, 2, 0),
            _ => new PopulationCohorts(52, 4, 1, 0)
        };

        return new RuntimeCity(
            node.Id,
            NameFor(node, index),
            node.X,
            node.Y,
            CityLevel.Hamlet,
            population,
            ["market", index == 0 ? "charter_house" : "trading_post"],
            market,
            warehouse,
            1.0);
    }

    private decimal RunProduction(int tick, ProductionReservation? reservation)
    {
        var cash = 0m;
        foreach (var city in _cities.OrderBy(city => city.Id, StringComparer.Ordinal))
        {
            var producedIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var recipe in _content.Recipes.OrderBy(recipe => recipe.Id, StringComparer.Ordinal))
            {
                if (!CanRunInCity(city, recipe, reservation))
                {
                    continue;
                }

                foreach (var input in recipe.Inputs)
                {
                    city.CompanyWarehouse.TryRemove(input.ResourceId, input.Amount);
                }

                foreach (var output in recipe.Outputs)
                {
                    city.CompanyWarehouse.Add(output.ResourceId, output.Amount);
                }

                producedIds.Add(recipe.Id);
            }

            if (producedIds.Count == 0)
            {
                continue;
            }

            var cityCash = 0m;
            foreach (var recipe in _content.Recipes.Where(recipe => producedIds.Contains(recipe.Id)))
            {
                foreach (var output in recipe.Outputs)
                {
                    var moved = Math.Min(output.Amount, city.CompanyWarehouse.Get(output.ResourceId));
                    if (moved <= 0)
                    {
                        continue;
                    }

                    city.CompanyWarehouse.TryRemove(output.ResourceId, moved);
                    city.Market.Add(output.ResourceId, moved);
                    cityCash += PriceFor(city, output.ResourceId) * moved * 0.05m;
                }
            }

            cityCash = RoundMoney(cityCash);
            cash += cityCash;
            _ledger.Add(new PrototypeLedgerEntry(tick, "Production", $"{city.Name}: {producedIds.Count} recipes produced", cityCash, city.Id));
        }

        return cash;
    }

    private decimal RunLogistics(int tick)
    {
        var selectedContract = SelectedAvailableContract();
        if (selectedContract is not null)
        {
            return ExecuteContract(tick, selectedContract, selected: true);
        }

        if (_selectedContractId is not null)
        {
            _selectedContractId = null;
        }

        var charter = _cities[0];
        var prices = PricesFor(charter);
        var needsByResource = _needs.ToDictionary(need => need.ResourceId, StringComparer.Ordinal);
        var cash = 0m;

        foreach (var route in Routes.Where(route => route.FromNode == charter.Id || route.ToNode == charter.Id).OrderBy(route => route.Id, StringComparer.Ordinal).Take(3))
        {
            var otherId = route.FromNode == charter.Id ? route.ToNode : route.FromNode;
            var source = _cities.First(city => city.Id == otherId);
            var candidates = source.CompanyWarehouse.Stock
                .Select(kvp => new
                {
                    kvp.Key,
                    Exportable = ExportableWarehouseUnits(source, kvp.Key),
                    Priority = ShipmentPriority(charter, kvp.Key)
                })
                .Where(kvp => kvp.Exportable > 0 && needsByResource.ContainsKey(kvp.Key))
                .OrderByDescending(kvp => kvp.Priority)
                .ThenByDescending(kvp => NeedScarcity(charter, needsByResource[kvp.Key]))
                .ThenBy(kvp => kvp.Key, StringComparer.Ordinal)
                .ToArray();

            if (candidates.Length == 0)
            {
                continue;
            }

            var candidate = candidates[0];
            var units = Math.Min(Math.Min(candidate.Exportable, 3), Math.Max(1, route.CapacityPerDay / 4));
            if (!source.CompanyWarehouse.TryRemove(candidate.Key, units))
            {
                continue;
            }

            charter.Market.Add(candidate.Key, units);
            prices.TryGetValue(candidate.Key, out var price);
            var revenue = price * units;
            var transportCost = route.CostPerUnit * units;
            var net = decimal.Round(revenue - transportCost, 2, MidpointRounding.AwayFromZero);
            cash += net;

            _ledger.Add(new PrototypeLedgerEntry(tick, "Logistics", $"{route.Id}: delivered {units} {candidate.Key} from {source.Name}", net, route.Id));
        }

        return cash;
    }

    private decimal ExecuteContract(int tick, PrototypeRouteContractView contract, bool selected)
    {
        var route = Routes.FirstOrDefault(route => route.Id == contract.RouteId);
        var source = _cities.FirstOrDefault(city => city.Id == contract.FromNode);
        var destination = _cities.FirstOrDefault(city => city.Id == contract.ToNode);
        if (route is null || source is null || destination is null)
        {
            return 0m;
        }

        var units = Math.Min(ExportableWarehouseUnits(source, contract.ResourceId), Math.Min(contract.Units, ContractUnits(route)));
        if (units <= 0 || !source.CompanyWarehouse.TryRemove(contract.ResourceId, units))
        {
            return 0m;
        }

        var prices = PricesFor(destination);
        prices.TryGetValue(contract.ResourceId, out var price);
        destination.Market.Add(contract.ResourceId, units);

        var revenue = price * units;
        var transportCost = route.CostPerUnit * units;
        var net = decimal.Round(revenue - transportCost, 2, MidpointRounding.AwayFromZero);
        var label = selected ? "selected contract" : "contract";
        _ledger.Add(new PrototypeLedgerEntry(tick, "Logistics", $"{route.Id}: {label} delivered {units} {contract.ResourceId} from {source.Name}", net, route.Id));
        return net;
    }

    private decimal RunAi(int tick)
    {
        var opportunities = BuildOpportunities().ToArray();
        if (opportunities.Length == 0)
        {
            _aiChoice = new OpportunityScore("none", 0m);
            return 0m;
        }

        _aiChoice = new CompanyUtilityAi().ChooseBest(opportunities);
        var pressure = _aiChoice.Score > 0 ? decimal.Round(Math.Min(8m, _aiChoice.Score * 0.02m), 2, MidpointRounding.AwayFromZero) : 0m;
        _ledger.Add(new PrototypeLedgerEntry(tick, "AI", $"Competitor favors {_aiChoice.OpportunityId}", -pressure, _aiChoice.OpportunityId));
        return -pressure;
    }

    private void RunCityGrowth(int tick)
    {
        foreach (var city in _cities.OrderBy(city => city.Id, StringComparer.Ordinal))
        {
            ConsumeNeeds(city.Market, _needs);
            var state = new CityState(city.Id, city.Level, city.Population, city.Districts, city.Market, city.CompanyWarehouse);
            var report = _cityGrowth.Evaluate(state, _needs);
            city.SupplySatisfaction = report.SupplySatisfaction;
            city.Level = report.Level;
            city.Population = ApplyPopulationDelta(city.Population, report.PopulationDelta);

            if (report.PopulationDelta != 0)
            {
                _ledger.Add(new PrototypeLedgerEntry(tick, "City", $"{city.Name}: population {Signed(report.PopulationDelta)}", 0m, city.Id));
            }
        }
    }

    private PrototypeSnapshot BuildSnapshot(int tick)
    {
        var charter = _cities[0];
        var prices = _economy.CalculatePrices(_content.Resources, charter.Market, _needs);
        var contracts = BuildAvailableContracts();
        var selectedContractId = contracts.Any(contract => contract.Id == _selectedContractId)
            ? _selectedContractId
            : null;
        _selectedContractId = selectedContractId;
        var save = BuildSave(prices, selectedContractId);
        var views = _cities
            .OrderBy(city => city.Id, StringComparer.Ordinal)
            .Select(city => new PrototypeCityView(
                city.Id,
                city.Name,
                city.X,
                city.Y,
                city.Level,
                city.Population.Total,
                city.SupplySatisfaction,
                city.Market.ToDictionary(),
                city.CompanyWarehouse.ToDictionary(),
                BuildMarketSignals(city)))
            .ToArray();

        return new PrototypeSnapshot(
            tick,
            World,
            _content.ContentHash,
            _content.Resources,
            Routes,
            prices,
            views,
            _company,
            _calendar,
            _aiChoice,
            _ledger.ToArray(),
            SaveCodec.ComputeStateHash(save))
        {
            AvailableContracts = contracts,
            SelectedContractId = selectedContractId
        };
    }

    private IReadOnlyList<PrototypeRouteContractView> BuildAvailableContracts()
    {
        var charter = _cities[0];
        var needsByResource = _needs.ToDictionary(need => need.ResourceId, StringComparer.Ordinal);
        var prices = PricesFor(charter);
        var contracts = new List<PrototypeRouteContractView>();

        foreach (var route in Routes
            .Where(route => route.FromNode == charter.Id || route.ToNode == charter.Id)
            .OrderBy(route => route.Id, StringComparer.Ordinal))
        {
            var sourceId = route.FromNode == charter.Id ? route.ToNode : route.FromNode;
            var source = _cities.FirstOrDefault(city => city.Id == sourceId);
            if (source is null)
            {
                continue;
            }

            foreach (var stock in source.CompanyWarehouse.Stock
                .Where(kvp => kvp.Value > 0 && needsByResource.ContainsKey(kvp.Key))
                .OrderBy(kvp => kvp.Key, StringComparer.Ordinal))
            {
                var units = Math.Min(ExportableWarehouseUnits(source, stock.Key), ContractUnits(route));
                if (units <= 0)
                {
                    continue;
                }

                prices.TryGetValue(stock.Key, out var price);
                var priority = ShipmentPriority(charter, stock.Key);
                var expectedRevenue = decimal.Round(price * units, 2, MidpointRounding.AwayFromZero);
                var transportCost = decimal.Round(route.CostPerUnit * units, 2, MidpointRounding.AwayFromZero);
                contracts.Add(new PrototypeRouteContractView(
                    $"{route.Id}:{stock.Key}",
                    route.Id,
                    source.Id,
                    charter.Id,
                    stock.Key,
                    expectedRevenue,
                    transportCost,
                    expectedRevenue - transportCost,
                    route.CapacityPerDay,
                    units,
                    priority,
                    PolicyAction(charter, stock.Key)));
            }
        }

        return contracts
            .OrderByDescending(contract => contract.ShipmentPriority)
            .ThenByDescending(contract => contract.ExpectedNet)
            .ThenBy(contract => contract.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private IReadOnlyList<PrototypeMarketSignal> BuildMarketSignals(RuntimeCity city)
    {
        var needsByResource = _needs.ToDictionary(need => need.ResourceId, StringComparer.Ordinal);
        var prices = _economy
            .CalculatePrices(_content.Resources, city.Market, _needs)
            .ToDictionary(price => price.ResourceId, price => price, StringComparer.Ordinal);
        var signals = new List<PrototypeMarketSignal>();

        foreach (var resource in _content.Resources.OrderBy(resource => resource.Id, StringComparer.Ordinal))
        {
            needsByResource.TryGetValue(resource.Id, out var need);
            var marketStock = city.Market.Get(resource.Id);
            var warehouseStock = city.CompanyWarehouse.Get(resource.Id);
            if (need is null && marketStock == 0 && warehouseStock == 0)
            {
                continue;
            }

            prices.TryGetValue(resource.Id, out var price);
            var desiredStock = Math.Max(0, need?.DesiredStock ?? 0);
            var consumption = Math.Max(0, need?.ConsumptionPerTick ?? 0);
            var safetyStock = SafetyStockFor(need);
            var reorderPoint = ReorderPointFor(need);
            var shipmentPriority = ShipmentPriority(marketStock, desiredStock, safetyStock, reorderPoint);
            signals.Add(new PrototypeMarketSignal(
                resource.Id,
                price?.Price ?? resource.BasePrice,
                price?.Scarcity ?? 0,
                marketStock,
                warehouseStock,
                desiredStock,
                consumption,
                safetyStock,
                reorderPoint,
                shipmentPriority,
                MarketReason(marketStock, warehouseStock, desiredStock, consumption, price?.Scarcity ?? 0, safetyStock, reorderPoint),
                PolicyAction(shipmentPriority, desiredStock, marketStock, consumption)));
        }

        return signals
            .OrderByDescending(signal => signal.ShipmentPriority)
            .ThenByDescending(signal => signal.Scarcity)
            .ThenBy(signal => signal.ResourceId, StringComparer.Ordinal)
            .ToArray();
    }

    private PrototypeRouteContractView? SelectedAvailableContract()
    {
        if (_selectedContractId is null)
        {
            return null;
        }

        return BuildAvailableContracts().FirstOrDefault(contract => contract.Id == _selectedContractId);
    }

    private ProductionReservation? ReservationFor(PrototypeRouteContractView? contract)
    {
        if (contract is null)
        {
            return null;
        }

        var route = Routes.FirstOrDefault(route => route.Id == contract.RouteId);
        var source = _cities.FirstOrDefault(city => city.Id == contract.FromNode);
        if (route is null || source is null)
        {
            return null;
        }

        var amount = Math.Min(ExportableWarehouseUnits(source, contract.ResourceId), ContractUnits(route));
        return amount <= 0 ? null : new ProductionReservation(source.Id, contract.ResourceId, amount);
    }

    private SaveGame BuildSave(IReadOnlyList<MarketPrice> prices, string? selectedContractId)
    {
        var priceState = prices.ToDictionary(price => price.ResourceId, price => price.Price, StringComparer.Ordinal);
        var cities = _cities.Select(city => new CitySaveState(
            city.Id,
            city.Level.ToString(),
            new Dictionary<string, int>
            {
                ["peasants"] = city.Population.Peasants,
                ["artisans"] = city.Population.Artisans,
                ["merchants"] = city.Population.Merchants,
                ["elite"] = city.Population.Elite
            },
            city.Districts,
            city.Market.ToDictionary(),
            city.CompanyWarehouse.ToDictionary(),
            priceState)).ToArray();

        return new SaveGame(
            SaveCodec.CurrentSaveVersion,
            _content.ContentHash,
            World.WorldGenVersion,
            World.Seed,
            new RngStreams((ulong)World.Seed, (ulong)World.Seed + 101, (ulong)World.Seed + 202),
            _calendar,
            _company,
            cities,
            Routes.Select(route => new RouteSaveState(route.Id, route.FromNode, route.ToNode, route.Mode, route.CapacityPerDay, ["food", "fuel", "construction"])).ToArray(),
            [],
            new FogOfWarState(_cities.Take(4).Select(city => city.Id).ToArray()),
            selectedContractId);
    }

    private IEnumerable<Opportunity> BuildOpportunities()
    {
        var charter = _cities[0];
        var charterPrices = PricesFor(charter);

        foreach (var route in Routes.OrderBy(route => route.Id, StringComparer.Ordinal))
        {
            var sourceId = route.FromNode == charter.Id ? route.ToNode : route.FromNode;
            var source = _cities.FirstOrDefault(city => city.Id == sourceId);
            if (source is null)
            {
                continue;
            }

            foreach (var resourceId in source.CompanyWarehouse.Stock.Keys.Order(StringComparer.Ordinal).Take(3))
            {
                charterPrices.TryGetValue(resourceId, out var price);
                var expectedRevenue = price * Math.Min(4, Math.Max(1, source.CompanyWarehouse.Get(resourceId)));
                var capitalCost = route.CostPerUnit * 2m;
                var risk = route.Mode == "coastal" ? 0.18 : 0.10;
                var volatility = resourceId is "tools" or "cloth" or "ceramics" ? 0.20 : 0.08;
                var bonus = resourceId is "grain" or "wood" or "fish" ? 6m : 2m;
                yield return new Opportunity($"{route.Id}:{resourceId}", route, resourceId, expectedRevenue, capitalCost, risk, volatility, bonus);
            }
        }
    }

    private Dictionary<string, decimal> PricesFor(RuntimeCity city)
    {
        return _economy
            .CalculatePrices(_content.Resources, city.Market, _needs)
            .ToDictionary(price => price.ResourceId, price => price.Price, StringComparer.Ordinal);
    }

    private decimal PriceFor(RuntimeCity city, string resourceId)
    {
        return PricesFor(city).TryGetValue(resourceId, out var price) ? price : 1m;
    }

    private double NeedScarcity(RuntimeCity city, MarketNeed need)
    {
        return Math.Clamp((need.DesiredStock - city.Market.Get(need.ResourceId)) / (double)Math.Max(1, need.DesiredStock), 0, 1);
    }

    private int ShipmentPriority(RuntimeCity city, string resourceId)
    {
        var need = NeedFor(resourceId);
        return ShipmentPriority(
            city.Market.Get(resourceId),
            Math.Max(0, need?.DesiredStock ?? 0),
            SafetyStockFor(need),
            ReorderPointFor(need));
    }

    private string PolicyAction(RuntimeCity city, string resourceId)
    {
        var need = NeedFor(resourceId);
        return PolicyAction(ShipmentPriority(city, resourceId), Math.Max(0, need?.DesiredStock ?? 0), city.Market.Get(resourceId), Math.Max(0, need?.ConsumptionPerTick ?? 0));
    }

    private int ExportableWarehouseUnits(RuntimeCity city, string resourceId)
    {
        return Math.Max(0, city.CompanyWarehouse.Get(resourceId) - WarehouseReserveFor(city, resourceId));
    }

    private int WarehouseReserveFor(RuntimeCity city, string resourceId)
    {
        var need = NeedFor(resourceId);
        if (need is null)
        {
            return 0;
        }

        return Math.Min(city.CompanyWarehouse.Get(resourceId), SafetyStockFor(need));
    }

    private MarketNeed? NeedFor(string resourceId)
    {
        return _needs.FirstOrDefault(need => string.Equals(need.ResourceId, resourceId, StringComparison.Ordinal));
    }

    private static int ContractUnits(TradeRoute route)
    {
        return Math.Max(1, Math.Min(3, route.CapacityPerDay / 4));
    }

    private static int SafetyStockFor(MarketNeed? need)
    {
        if (need is null)
        {
            return 0;
        }

        return Math.Max(need.ConsumptionPerTick * 2, (int)Math.Ceiling(need.DesiredStock * 0.25));
    }

    private static int ReorderPointFor(MarketNeed? need)
    {
        if (need is null)
        {
            return 0;
        }

        return Math.Max(SafetyStockFor(need) + need.ConsumptionPerTick, (int)Math.Ceiling(need.DesiredStock * 0.50));
    }

    private static int ShipmentPriority(int marketStock, int desiredStock, int safetyStock, int reorderPoint)
    {
        if (desiredStock <= 0)
        {
            return 0;
        }

        if (marketStock <= 0)
        {
            return 4;
        }

        if (marketStock < safetyStock)
        {
            return 3;
        }

        if (marketStock < reorderPoint)
        {
            return 2;
        }

        return marketStock < desiredStock ? 1 : 0;
    }

    public static int ConsumeNeeds(Inventory market, IEnumerable<MarketNeed> needs)
    {
        var consumed = 0;
        foreach (var need in needs.OrderBy(need => need.ResourceId, StringComparer.Ordinal))
        {
            var amount = Math.Min(market.Get(need.ResourceId), Math.Max(0, need.ConsumptionPerTick));
            if (amount > 0 && market.TryRemove(need.ResourceId, amount))
            {
                consumed += amount;
            }
        }

        return consumed;
    }

    private static bool CanRunInCity(RuntimeCity city, RecipeDef recipe, ProductionReservation? reservation)
    {
        if (recipe.Inputs.Count == 0)
        {
            return recipe.Outputs.Any(output => city.CompanyWarehouse.Get(output.ResourceId) > 0 || city.Market.Get(output.ResourceId) > 0);
        }

        return recipe.Inputs.All(input => AvailableForProduction(city, input.ResourceId, reservation) >= input.Amount);
    }

    private static int AvailableForProduction(RuntimeCity city, string resourceId, ProductionReservation? reservation)
    {
        var stock = city.CompanyWarehouse.Get(resourceId);
        if (reservation is null || reservation.CityId != city.Id || reservation.ResourceId != resourceId)
        {
            return stock;
        }

        return Math.Max(0, stock - reservation.Amount);
    }

    private static PopulationCohorts ApplyPopulationDelta(PopulationCohorts population, int delta)
    {
        return population with { Peasants = Math.Max(0, population.Peasants + delta) };
    }

    private static string NameFor(WorldNode node, int index)
    {
        var prefix = node.Kind switch
        {
            "charter_town" => "Charter",
            "port" => "Port",
            _ => "Market"
        };
        return $"{prefix} {index + 1}";
    }

    private static string Signed(int value)
    {
        return value >= 0 ? $"+{value}" : value.ToString();
    }

    private static decimal RoundMoney(decimal value)
    {
        return decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    private static string PolicyAction(int shipmentPriority, int desiredStock, int marketStock, int consumptionPerTick)
    {
        return shipmentPriority switch
        {
            4 => "emergency shipment",
            3 => "protect safety stock",
            2 => "reorder now",
            1 => "top up when capacity allows",
            _ => desiredStock > 0 && consumptionPerTick > 0 && marketStock >= desiredStock + consumptionPerTick * 2
                ? "surplus; export above target"
                : "hold"
        };
    }

    private static string MarketReason(int marketStock, int warehouseStock, int desiredStock, int consumptionPerTick, double scarcity, int safetyStock, int reorderPoint)
    {
        if (desiredStock <= 0)
        {
            return warehouseStock > 0 ? "export stock only" : "nonessential";
        }

        if (marketStock <= 0)
        {
            return warehouseStock > 0
                ? $"stockout; {warehouseStock} held in company warehouse"
                : "stockout; no local buffer";
        }

        if (consumptionPerTick > 0 && marketStock < consumptionPerTick)
        {
            return $"critical; less than one tick of demand";
        }

        if (marketStock < safetyStock)
        {
            return $"below safety stock {safetyStock}";
        }

        if (marketStock < reorderPoint)
        {
            return $"below reorder point {reorderPoint}";
        }

        if (marketStock < desiredStock)
        {
            return $"short {desiredStock - marketStock} vs target";
        }

        if (scarcity <= -0.35)
        {
            return "surplus; export candidate";
        }

        return "stable";
    }

    private sealed class RuntimeCity(
        string id,
        string name,
        int x,
        int y,
        CityLevel level,
        PopulationCohorts population,
        IReadOnlyList<string> districts,
        Inventory market,
        Inventory companyWarehouse,
        double supplySatisfaction)
    {
        public string Id { get; } = id;
        public string Name { get; } = name;
        public int X { get; } = x;
        public int Y { get; } = y;
        public CityLevel Level { get; set; } = level;
        public PopulationCohorts Population { get; set; } = population;
        public IReadOnlyList<string> Districts { get; } = districts;
        public Inventory Market { get; } = market;
        public Inventory CompanyWarehouse { get; } = companyWarehouse;
        public double SupplySatisfaction { get; set; } = supplySatisfaction;
    }

    private sealed record ProductionReservation(string CityId, string ResourceId, int Amount);
}
