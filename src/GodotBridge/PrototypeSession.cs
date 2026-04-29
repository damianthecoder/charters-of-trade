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
    IReadOnlyDictionary<string, int> CompanyWarehouse);

public sealed record PrototypeLedgerEntry(
    int Tick,
    string Category,
    string Message,
    decimal CashDelta,
    string RelatedId);

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

    public PrototypeSession(GeneratedWorld world, GameContent content, IReadOnlyList<TradeRoute> routes)
    {
        World = world;
        _content = content;
        Routes = routes;
        _needs = StarterScenarioFactory.CreateNeeds(content.Resources);
        _cities = world.Nodes.OrderBy(node => node.Id, StringComparer.Ordinal).Select(CreateCity).ToList();
        Current = BuildSnapshot();
    }

    public GeneratedWorld World { get; }

    public IReadOnlyList<TradeRoute> Routes { get; }

    public PrototypeSnapshot Current { get; private set; }

    public PrototypeSnapshot AdvanceTick()
    {
        var nextTick = Current.Tick + 1;
        _calendar = _calendar with { DayOfYear = _calendar.DayOfYear + 1 };

        var productionCash = RunProduction(nextTick);
        var logisticsCash = RunLogistics(nextTick);
        var aiCash = RunAi(nextTick);
        RunCityGrowth(nextTick);

        var cashDelta = productionCash + logisticsCash + aiCash;
        _company = _company with { Cash = decimal.Round(_company.Cash + cashDelta, 2, MidpointRounding.AwayFromZero) };

        Current = BuildSnapshot();
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

    private decimal RunProduction(int tick)
    {
        var cash = 0m;
        foreach (var city in _cities.OrderBy(city => city.Id, StringComparer.Ordinal))
        {
            var recipes = _content.Recipes.Where(recipe => CanRunInCity(city, recipe)).ToArray();
            var results = _economy.RunProduction(city.CompanyWarehouse, recipes);
            var producedIds = results
                .Where(result => result.Produced)
                .Select(result => result.RecipeId)
                .ToHashSet(StringComparer.Ordinal);

            if (producedIds.Count == 0)
            {
                continue;
            }

            var cityCash = 0m;
            foreach (var recipe in recipes.Where(recipe => producedIds.Contains(recipe.Id)))
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

            cash += cityCash;
            _ledger.Add(new PrototypeLedgerEntry(tick, "Production", $"{city.Name}: {producedIds.Count} recipes produced", cityCash, city.Id));
        }

        return cash;
    }

    private decimal RunLogistics(int tick)
    {
        var charter = _cities[0];
        var prices = PricesFor(charter);
        var needsByResource = _needs.ToDictionary(need => need.ResourceId, StringComparer.Ordinal);
        var cash = 0m;

        foreach (var route in Routes.Where(route => route.FromNode == charter.Id || route.ToNode == charter.Id).OrderBy(route => route.Id, StringComparer.Ordinal).Take(3))
        {
            var otherId = route.FromNode == charter.Id ? route.ToNode : route.FromNode;
            var source = _cities.First(city => city.Id == otherId);
            var candidate = source.CompanyWarehouse.Stock
                .Where(kvp => kvp.Value > 0 && needsByResource.ContainsKey(kvp.Key))
                .OrderByDescending(kvp => NeedScarcity(charter, needsByResource[kvp.Key]))
                .ThenBy(kvp => kvp.Key, StringComparer.Ordinal)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(candidate.Key))
            {
                continue;
            }

            var units = Math.Min(Math.Min(candidate.Value, 3), Math.Max(1, route.CapacityPerDay / 4));
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

    private PrototypeSnapshot BuildSnapshot()
    {
        var charter = _cities[0];
        var prices = _economy.CalculatePrices(_content.Resources, charter.Market, _needs);
        var save = BuildSave(prices);
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
                city.CompanyWarehouse.ToDictionary()))
            .ToArray();

        return new PrototypeSnapshot(
            Current?.Tick + 1 ?? 0,
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
            SaveCodec.ComputeStateHash(save));
    }

    private SaveGame BuildSave(IReadOnlyList<MarketPrice> prices)
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
            new FogOfWarState(_cities.Take(4).Select(city => city.Id).ToArray()));
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

    private static bool CanRunInCity(RuntimeCity city, RecipeDef recipe)
    {
        if (recipe.Inputs.Count == 0)
        {
            return recipe.Outputs.Any(output => city.CompanyWarehouse.Get(output.ResourceId) > 0 || city.Market.Get(output.ResourceId) > 0);
        }

        return recipe.Inputs.All(input => city.CompanyWarehouse.Get(input.ResourceId) >= input.Amount);
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
}
