using System.Globalization;
using ChartersOfTrade.AI.Company;
using ChartersOfTrade.Content.Core;
using ChartersOfTrade.Economy.Core;
using ChartersOfTrade.GodotBridge;
using ChartersOfTrade.Logistics.Core;
using ChartersOfTrade.Persistence.Core;
using ChartersOfTrade.WorldGen.Core;

var tests = new (string Name, Action Run)[]
{
    ("world generation is deterministic", WorldGenerationIsDeterministic),
    ("world hash is culture invariant", WorldHashIsCultureInvariant),
    ("world hash includes terrain raster", WorldHashIncludesTerrainRaster),
    ("generated world has a solvency kernel", GeneratedWorldHasSolvencyKernel),
    ("P0 content loads and validates", P0ContentLoadsAndValidates),
    ("content validation rejects unknown recipe resources", ContentValidationRejectsUnknownRecipeResources),
    ("simulation bridge uses loaded content", SimulationBridgeUsesLoadedContent),
    ("prototype session ticks deterministically", PrototypeSessionTicksDeterministically),
    ("prototype session advances all systems", PrototypeSessionAdvancesAllSystems),
    ("prototype consumption uses declared market needs", PrototypeConsumptionUsesDeclaredMarketNeeds),
    ("economy production never creates negative stock", EconomyProductionNeverCreatesNegativeStock),
    ("save-load-save preserves hash", SaveLoadSavePreservesHash),
    ("save load rejects negative stock", SaveLoadRejectsNegativeStock),
    ("AI chooses the highest utility opportunity", AiChoosesHighestUtilityOpportunity)
};

var failures = new List<string>();

foreach (var test in tests)
{
    try
    {
        test.Run();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception ex)
    {
        failures.Add($"{test.Name}: {ex.Message}");
        Console.WriteLine($"FAIL {test.Name}: {ex.Message}");
    }
}

if (failures.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("Failures:");
    foreach (var failure in failures)
    {
        Console.WriteLine($"- {failure}");
    }

    return 1;
}

Console.WriteLine();
Console.WriteLine($"All {tests.Length} tests passed.");
return 0;

static void WorldGenerationIsDeterministic()
{
    var generator = new WorldGenerator();
    var first = generator.Generate(new WorldGenConfig(128734221));
    var second = generator.Generate(new WorldGenConfig(128734221));
    AssertEqual(first.Hash, second.Hash);
    AssertEqual(first.Nodes.Count, second.Nodes.Count);
    AssertEqual(first.Edges.Count, second.Edges.Count);
}

static void GeneratedWorldHasSolvencyKernel()
{
    var world = new WorldGenerator().Generate(new WorldGenConfig(777));
    AssertTrue(world.HasSolvencyKernel, "Expected a food + wood + connected route solvency kernel.");
}

static void WorldHashIsCultureInvariant()
{
    var originalCulture = CultureInfo.CurrentCulture;
    var originalUiCulture = CultureInfo.CurrentUICulture;

    try
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pl-PL");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("pl-PL");
        var polishHash = new WorldGenerator().Generate(new WorldGenConfig(128734221)).Hash;

        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        var invariantHash = new WorldGenerator().Generate(new WorldGenConfig(128734221)).Hash;

        AssertEqual(polishHash, invariantHash);
    }
    finally
    {
        CultureInfo.CurrentCulture = originalCulture;
        CultureInfo.CurrentUICulture = originalUiCulture;
    }
}

static void WorldHashIncludesTerrainRaster()
{
    var config = new WorldGenConfig(1234, Width: 8, Height: 8, SettlementCount: 2);
    var nodes = new[]
    {
        new WorldNode("node_001", "charter_town", 2, 2, "north_west", ["grain", "wood"], 80),
        new WorldNode("node_002", "market_town", 5, 5, "south_east", ["fish", "clay"], 70)
    };
    var edges = new[]
    {
        new WorldEdge("edge_001", "node_001", "node_002", "road", 4.2, 4.2, 12)
    };
    var terrain = new[]
    {
        new TerrainCell(0, 0, 0.20, 0.40, 0.50, false)
    };
    var wetterTerrain = new[]
    {
        terrain[0] with { Moisture = 0.90 }
    };

    var first = WorldHasher.Compute(config, terrain, nodes, edges);
    var second = WorldHasher.Compute(config, wetterTerrain, nodes, edges);
    AssertTrue(first != second, "Terrain changes should affect world hash.");
}

static void EconomyProductionNeverCreatesNegativeStock()
{
    var inventory = new Inventory(new Dictionary<string, int> { ["grain"] = 2, ["wood"] = 0 });
    var recipes = new[]
    {
        new RecipeDef("bake_bread", "bakery", [new ResourceAmount("grain", 2)], [new ResourceAmount("bread", 1)], new WorkforceRequirement(2, 1), 7, "milling"),
        new RecipeDef("forge_tools", "smithy", [new ResourceAmount("iron", 1), new ResourceAmount("wood", 1)], [new ResourceAmount("tools", 1)], new WorkforceRequirement(1, 2), 10, "smithing")
    };

    var results = new EconomyTick().RunProduction(inventory, recipes);
    AssertTrue(results.Any(result => result.RecipeId == "bake_bread" && result.Produced), "Bread recipe should produce.");
    AssertTrue(results.Any(result => result.RecipeId == "forge_tools" && !result.Produced), "Tools recipe should fail without inputs.");
    AssertTrue(inventory.Stock.Values.All(amount => amount >= 0), "Inventory contains a negative stock value.");
}

static void P0ContentLoadsAndValidates()
{
    var content = GameContentLoader.LoadFromDirectory(ContentPathResolver.FindContentDirectory());
    AssertEqual(10, content.Resources.Count);
    AssertEqual(6, content.Recipes.Count);
    AssertTrue(content.ContentHash.StartsWith("sha256:", StringComparison.Ordinal), "Content hash should be sha256-prefixed.");
}

static void ContentValidationRejectsUnknownRecipeResources()
{
    var resourcesJson = """
        [
          { "id": "grain", "tier": "staple", "tags": ["food"], "basePrice": 10, "weight": 1.0, "spoilDays": 0, "substitutes": [] }
        ]
        """;
    var recipesJson = """
        [
          { "id": "bad_recipe", "buildingType": "bakery", "inputs": [{ "resourceId": "missing", "amount": 1 }], "outputs": [{ "resourceId": "grain", "amount": 1 }], "workforce": { "peasants": 1, "artisans": 0 }, "baseDays": 1, "requiresTech": "" }
        ]
        """;

    try
    {
        GameContentLoader.Load(resourcesJson, recipesJson);
        throw new InvalidOperationException("Expected content validation to fail.");
    }
    catch (ContentValidationException ex)
    {
        AssertTrue(ex.Errors.Any(error => error.Contains("unknown input resource", StringComparison.Ordinal)), "Expected unknown input resource error.");
    }
}

static void SimulationBridgeUsesLoadedContent()
{
    var content = GameContentLoader.LoadFromDirectory(ContentPathResolver.FindContentDirectory());
    var snapshot = new SimulationBridge().CreateNewGame(424242);
    AssertEqual(content.ContentHash, snapshot.ContentHash);
    AssertEqual(content.Resources.Count, snapshot.InitialPrices.Count);
}

static void PrototypeSessionTicksDeterministically()
{
    var bridge = new SimulationBridge();
    var first = bridge.CreatePrototypeSession(20260429);
    var second = bridge.CreatePrototypeSession(20260429);

    for (var i = 0; i < 5; i++)
    {
        first.AdvanceTick();
        second.AdvanceTick();
    }

    AssertEqual(first.Current.SaveHash, second.Current.SaveHash);
    AssertEqual(first.Current.Company.Cash, second.Current.Company.Cash);
    AssertEqual(first.Current.AiChoice, second.Current.AiChoice);
}

static void PrototypeSessionAdvancesAllSystems()
{
    var session = new SimulationBridge().CreatePrototypeSession(424242);
    var initial = session.Current;
    var tick = session.AdvanceTick();

    AssertEqual(initial.World.Hash, tick.World.Hash);
    AssertEqual(1, tick.Tick);
    AssertEqual(2, tick.Calendar.DayOfYear);
    AssertTrue(tick.Cities.Count == tick.World.Nodes.Count, "Expected one prototype city per world node.");
    AssertTrue(tick.SaveHash != initial.SaveHash, "Expected save hash to change after a tick.");
    AssertTrue(tick.Ledger.Any(entry => entry.Category == "Production"), "Expected production ledger entry.");
    AssertTrue(tick.Ledger.Any(entry => entry.Category == "Logistics"), "Expected logistics ledger entry.");
    AssertTrue(tick.Ledger.Any(entry => entry.Category == "AI"), "Expected AI ledger entry.");
    AssertEqual(tick.Company.Cash - initial.Company.Cash, tick.Ledger.Where(entry => entry.Tick == tick.Tick).Sum(entry => entry.CashDelta));
    AssertTrue(tick.Cities.All(city => city.MarketStock.Values.All(amount => amount >= 0)), "Market contains negative stock.");
    AssertTrue(tick.Cities.All(city => city.CompanyWarehouse.Values.All(amount => amount >= 0)), "Warehouse contains negative stock.");
}

static void PrototypeConsumptionUsesDeclaredMarketNeeds()
{
    var market = new Inventory(new Dictionary<string, int> { ["grain"] = 5, ["wood"] = 1, ["tools"] = 0 });
    var consumed = PrototypeSession.ConsumeNeeds(market,
    [
        new MarketNeed("grain", 10, 2),
        new MarketNeed("wood", 5, 3),
        new MarketNeed("tools", 4, 1)
    ]);

    AssertEqual(3, consumed);
    AssertEqual(3, market.Get("grain"));
    AssertEqual(0, market.Get("wood"));
    AssertEqual(0, market.Get("tools"));
}

static void SaveLoadSavePreservesHash()
{
    var content = GameContentLoader.LoadFromDirectory(ContentPathResolver.FindContentDirectory());
    var bridge = new SimulationBridge();
    var snapshot = bridge.CreateNewGame(424242);
    var routes = RoutePlanner.FromWorld(snapshot.World);
    var market = StarterScenarioFactory.CreateInitialMarket(content.Resources);
    var needs = StarterScenarioFactory.CreateNeeds(content.Resources);
    var prices = new EconomyTick().CalculatePrices(content.Resources, market, needs);
    var save = StarterSaveFactory.Create(424242, snapshot.World.WorldGenVersion, content.ContentHash, snapshot.World.Nodes, routes, market, prices);
    var firstHash = SaveCodec.ComputeStateHash(save);
    var json = SaveCodec.Serialize(save);
    var loaded = SaveCodec.Deserialize(json);
    var secondJson = SaveCodec.Serialize(loaded);
    var secondHash = SaveCodec.ComputeStateHash(SaveCodec.Deserialize(secondJson));
    AssertEqual(firstHash, secondHash);
}

static void SaveLoadRejectsNegativeStock()
{
    var content = GameContentLoader.LoadFromDirectory(ContentPathResolver.FindContentDirectory());
    var snapshot = new SimulationBridge().CreateNewGame(424242);
    var routes = RoutePlanner.FromWorld(snapshot.World);
    var market = StarterScenarioFactory.CreateInitialMarket(content.Resources);
    var prices = new EconomyTick().CalculatePrices(content.Resources, market, StarterScenarioFactory.CreateNeeds(content.Resources));
    var save = StarterSaveFactory.Create(424242, snapshot.World.WorldGenVersion, content.ContentHash, snapshot.World.Nodes, routes, market, prices);
    var invalid = save with
    {
        Cities =
        [
            save.Cities[0] with
            {
                MarketStock = new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    ["grain"] = -1
                }
            }
        ]
    };

    try
    {
        SaveCodec.Deserialize(SaveCodec.Serialize(invalid));
        throw new InvalidOperationException("Expected save validation to reject negative stock.");
    }
    catch (SaveValidationException ex)
    {
        AssertTrue(ex.Errors.Any(error => error.Contains("must not be negative", StringComparison.Ordinal)), "Expected negative stock validation error.");
    }
}

static void AiChoosesHighestUtilityOpportunity()
{
    var route = new TradeRoute("route_001", "node_001", "node_002", "road", 12, 3, 1.5m);
    var ai = new CompanyUtilityAi();
    var best = ai.ChooseBest(
    [
        new Opportunity("safe_grain", route, "grain", 80m, 20m, 0.1, 0.1, 0m),
        new Opportunity("luxury_push", route, "cloth", 140m, 25m, 0.2, 0.2, 15m)
    ]);

    AssertEqual("luxury_push", best.OpportunityId);
}

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected {expected}, got {actual}.");
    }
}

static void AssertTrue(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
