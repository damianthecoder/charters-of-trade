using ChartersOfTrade.AI.Company;
using ChartersOfTrade.CitySim.Core;
using ChartersOfTrade.Content.Core;
using ChartersOfTrade.Economy.Core;
using ChartersOfTrade.Logistics.Core;
using ChartersOfTrade.Persistence.Core;
using ChartersOfTrade.WorldGen.Core;

namespace ChartersOfTrade.GodotBridge;

public sealed record NewGameSnapshot(
    GeneratedWorld World,
    string ContentHash,
    IReadOnlyList<TradeRoute> Routes,
    IReadOnlyList<MarketPrice> InitialPrices,
    string SaveHash);

public sealed class SimulationBridge
{
    public NewGameSnapshot CreateNewGame(int seed)
    {
        return CreateNewGame(seed, ContentPathResolver.FindContentDirectory());
    }

    public NewGameSnapshot CreateNewGame(int seed, string contentDirectory)
    {
        var world = new WorldGenerator().Generate(new WorldGenConfig(seed));
        var content = GameContentLoader.LoadFromDirectory(contentDirectory);
        var routes = RoutePlanner.FromWorld(world);
        var market = StarterScenarioFactory.CreateInitialMarket(content.Resources);
        var needs = StarterScenarioFactory.CreateNeeds(content.Resources);
        var prices = new EconomyTick().CalculatePrices(content.Resources, market, needs);

        var save = StarterSaveFactory.Create(seed, world.WorldGenVersion, content.ContentHash, world.Nodes, routes, content.Resources, market, prices);
        return new NewGameSnapshot(world, content.ContentHash, routes, prices, SaveCodec.ComputeStateHash(save));
    }

    public PrototypeSession CreatePrototypeSession(int seed)
    {
        return CreatePrototypeSession(seed, ContentPathResolver.FindContentDirectory());
    }

    public PrototypeSession CreatePrototypeSession(int seed, string contentDirectory)
    {
        var world = new WorldGenerator().Generate(new WorldGenConfig(seed));
        var content = GameContentLoader.LoadFromDirectory(contentDirectory);
        var routes = RoutePlanner.FromWorld(world);
        return new PrototypeSession(world, content, routes);
    }
}

public static class ContentPathResolver
{
    public static string FindContentDirectory()
    {
        var candidates = new List<string>
        {
            Path.Combine(Environment.CurrentDirectory, "content"),
            Path.Combine(AppContext.BaseDirectory, "content"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "content")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "content"))
        };

        AddUpwardCandidates(candidates, Environment.CurrentDirectory);
        AddUpwardCandidates(candidates, AppContext.BaseDirectory);

        foreach (var candidate in candidates)
        {
            if (File.Exists(Path.Combine(candidate, "resources.p0.json"))
                && File.Exists(Path.Combine(candidate, "recipes.p0.json")))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException("Could not locate content directory containing resources.p0.json and recipes.p0.json.");
    }

    private static void AddUpwardCandidates(List<string> candidates, string start)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(start));
        while (directory is not null)
        {
            candidates.Add(Path.Combine(directory.FullName, "content"));
            directory = directory.Parent;
        }
    }
}

public static class StarterScenarioFactory
{
    public static Inventory CreateInitialMarket(IReadOnlyList<ResourceDef> resources)
    {
        var stock = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var resource in resources)
        {
            if (resource.Tags.Contains("food"))
            {
                stock[resource.Id] = resource.Tier == "staple" ? 12 : 4;
            }
            else if (resource.Tags.Contains("fuel") || resource.Tags.Contains("construction"))
            {
                stock[resource.Id] = 8;
            }
            else if (resource.Tier == "regional")
            {
                stock[resource.Id] = 3;
            }
            else
            {
                stock[resource.Id] = 0;
            }
        }

        return new Inventory(stock);
    }

    public static IReadOnlyList<MarketNeed> CreateNeeds(IReadOnlyList<ResourceDef> resources)
    {
        return resources
            .Where(resource => resource.Tags.Contains("food")
                || resource.Tags.Contains("fuel")
                || resource.Tags.Contains("construction")
                || resource.Tags.Contains("clothing")
                || resource.Tags.Contains("industry"))
            .OrderBy(resource => resource.Id, StringComparer.Ordinal)
            .Select(resource =>
            {
                var desired = resource.Tier switch
                {
                    "staple" => 16,
                    "regional" => 8,
                    _ => 6
                };
                var consumption = resource.Tags.Contains("food") ? 2 : 1;
                return new MarketNeed(resource.Id, desired, consumption);
            })
            .ToArray();
    }
}

public static class StarterSaveFactory
{
    public static SaveGame Create(
        int seed,
        string worldGenVersion,
        string contentHash,
        IReadOnlyList<WorldNode> nodes,
        IReadOnlyList<TradeRoute> routes,
        IReadOnlyList<ResourceDef> resources,
        Inventory initialMarket,
        IReadOnlyList<MarketPrice> initialPrices)
    {
        var priceState = initialPrices.ToDictionary(price => price.ResourceId, price => price.Price, StringComparer.Ordinal);
        var city = new CitySaveState(
            nodes[0].Id,
            CityLevel.Hamlet.ToString(),
            new Dictionary<string, int> { ["peasants"] = 90, ["artisans"] = 12, ["merchants"] = 4, ["elite"] = 0 },
            ["market", "granary"],
            initialMarket.ToDictionary(),
            initialMarket.Stock.Where(kvp => kvp.Value > 0).Take(3).ToDictionary(kvp => kvp.Key, kvp => Math.Max(1, kvp.Value / 2), StringComparer.Ordinal),
            priceState);

        var routePolicyResources = StarterScenarioFactory.CreateNeeds(resources)
            .Select(need => need.ResourceId)
            .Order(StringComparer.Ordinal)
            .ToArray();

        return new SaveGame(
            SaveCodec.CurrentSaveVersion,
            contentHash,
            worldGenVersion,
            seed,
            new RngStreams((ulong)seed, (ulong)seed + 101, (ulong)seed + 202),
            new CalendarState(1, 1),
            new CompanyState(1000m, 0m, 50, "merchant_league"),
            [city],
            routes.Select(route => new RouteSaveState(route.Id, route.FromNode, route.ToNode, route.Mode, route.CapacityPerDay, routePolicyResources)).ToArray(),
            [],
            new FogOfWarState(nodes.Take(3).Select(node => node.Id).ToArray()),
            [],
            routes.Select(route => new RoutePolicySaveState(route.Id, routePolicyResources, null)).ToArray(),
            null);
    }
}
