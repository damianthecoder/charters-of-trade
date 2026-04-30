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
    ("generated terrain forms readable landmass", GeneratedTerrainFormsReadableLandmass),
    ("dense settlement configs keep unique nodes", DenseSettlementConfigsKeepUniqueNodes),
    ("impossible settlement configs fail clearly", ImpossibleSettlementConfigsFailClearly),
    ("generated world has a solvency kernel", GeneratedWorldHasSolvencyKernel),
    ("P0 content loads and validates", P0ContentLoadsAndValidates),
    ("content validation rejects unknown recipe resources", ContentValidationRejectsUnknownRecipeResources),
    ("simulation bridge uses loaded content", SimulationBridgeUsesLoadedContent),
    ("prototype session ticks deterministically", PrototypeSessionTicksDeterministically),
    ("prototype session advances all systems", PrototypeSessionAdvancesAllSystems),
    ("prototype route contracts are deterministic", PrototypeRouteContractsAreDeterministic),
    ("prototype route contract selection affects logistics", PrototypeRouteContractSelectionAffectsLogistics),
    ("prototype route contract rejects invalid ids", PrototypeRouteContractRejectsInvalidIds),
    ("prototype selected route contract stays deterministic", PrototypeSelectedRouteContractStaysDeterministic),
    ("prototype route policy hash is deterministic", PrototypeRoutePolicyHashIsDeterministic),
    ("prototype route resource reservation filters contracts", PrototypeRouteResourceReservationFiltersContracts),
    ("prototype route priority boosts contract ordering", PrototypeRoutePriorityBoostsContractOrdering),
    ("prototype route policy invalid targets are no-ops", PrototypeRoutePolicyInvalidTargetsAreNoOps),
    ("prototype consumption uses declared market needs", PrototypeConsumptionUsesDeclaredMarketNeeds),
    ("economy prices respond to stock pressure", EconomyPricesRespondToStockPressure),
    ("prototype exposes local market pressure signals", PrototypeExposesLocalMarketPressureSignals),
    ("prototype warehouse policies expose safety stock", PrototypeWarehousePoliciesExposeSafetyStock),
    ("prototype warehouse policy overrides are deterministic", PrototypeWarehousePolicyOverridesAreDeterministic),
    ("prototype warehouse policy changes save hash", PrototypeWarehousePolicyChangesSaveHash),
    ("prototype warehouse policy clamps and rejects invalid targets", PrototypeWarehousePolicyClampsAndRejectsInvalidTargets),
    ("prototype warehouse policy affects contract priority and availability", PrototypeWarehousePolicyAffectsContractPriorityAndAvailability),
    ("prototype route contracts follow shipment priority", PrototypeRouteContractsFollowShipmentPriority),
    ("economy production never creates negative stock", EconomyProductionNeverCreatesNegativeStock),
    ("save-load-save preserves hash", SaveLoadSavePreservesHash),
    ("warehouse policy save-load preserves hash", WarehousePolicySaveLoadPreservesHash),
    ("route policy save validation rejects orphan priority", RoutePolicySaveValidationRejectsOrphanPriority),
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

static void GeneratedTerrainFormsReadableLandmass()
{
    foreach (var seed in Enumerable.Range(1000, 25).Concat([777, 424242, 20260429]))
    {
        var world = new WorldGenerator().Generate(new WorldGenConfig(seed));
        var landCount = world.Terrain.Count(cell => !cell.IsWater);
        var landRatio = landCount / (double)world.Terrain.Count;
        AssertTrue(landRatio > 0.42 && landRatio < 0.84, $"Expected readable land/water balance for seed {seed}, got {landRatio:0.00}.");
        AssertTrue(world.Terrain.Where(cell => cell.X == 0 || cell.Y == 0 || cell.X == world.Width - 1 || cell.Y == world.Height - 1).All(cell => cell.IsWater), $"Expected seed {seed} to keep map borders water for coastline readability.");
        AssertTrue(world.Terrain.Any(cell => !cell.IsWater && !TouchesWater(world, cell)), $"Expected seed {seed} to contain inland terrain, not only coast.");
        AssertTrue(world.Terrain.Count(cell => !cell.IsWater && TouchesWater(world, cell)) >= Math.Max(8, world.Width / 2), $"Expected seed {seed} to expose visible coastline cells.");
        foreach (var left in world.Nodes)
        {
            foreach (var right in world.Nodes.Where(node => string.CompareOrdinal(left.Id, node.Id) < 0))
            {
                AssertTrue(NodeDistance(left, right) >= 3.0, $"Expected settlements {left.Id} and {right.Id} in seed {seed} to stay visually separated.");
            }
        }

        foreach (var port in world.Nodes.Where(node => node.Kind == "port"))
        {
            AssertTrue(TouchesWaterAt(world, port.X, port.Y), $"Expected port {port.Id} in seed {seed} to be placed on a coast.");
        }

        foreach (var edge in world.Edges.Where(edge => edge.Mode == "coastal"))
        {
            var from = world.Nodes.Single(node => node.Id == edge.FromNode);
            var to = world.Nodes.Single(node => node.Id == edge.ToNode);
            AssertTrue(from.Kind == "port" && to.Kind == "port", $"Expected coastal edge {edge.Id} in seed {seed} to connect two ports.");
        }
    }
}

static void DenseSettlementConfigsKeepUniqueNodes()
{
    var world = new WorldGenerator().Generate(new WorldGenConfig(424242, SettlementCount: 64));
    var uniqueNodeCells = world.Nodes
        .Select(node => (node.X, node.Y))
        .Distinct()
        .Count();

    AssertEqual(64, world.Nodes.Count);
    AssertEqual(world.Nodes.Count, uniqueNodeCells);
    AssertTrue(world.Nodes.All(node => !IsWater(world, node.X, node.Y)), "Expected all dense-config settlements to remain on land.");
}

static void ImpossibleSettlementConfigsFailClearly()
{
    try
    {
        _ = new WorldGenerator().Generate(new WorldGenConfig(424242, SettlementCount: 1_000));
    }
    catch (ArgumentException ex) when (ex.Message.Contains("cannot place", StringComparison.Ordinal))
    {
        return;
    }

    throw new InvalidOperationException("Expected impossible settlement count to fail with a clear argument error.");
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

static void EconomyPricesRespondToStockPressure()
{
    var grain = new ResourceDef("grain", "staple", ["food"], 10m, 1.0, 0, []);
    var needs = new[] { new MarketNeed("grain", 10, 2) };
    var economy = new EconomyTick();

    var stockout = economy.CalculatePrices([grain], new Inventory(new Dictionary<string, int> { ["grain"] = 0 }), needs).Single();
    var balanced = economy.CalculatePrices([grain], new Inventory(new Dictionary<string, int> { ["grain"] = 10 }), needs).Single();
    var surplus = economy.CalculatePrices([grain], new Inventory(new Dictionary<string, int> { ["grain"] = 24 }), needs).Single();

    AssertTrue(stockout.Price > balanced.Price, "Stockout should price above balanced stock.");
    AssertTrue(surplus.Price < balanced.Price, "Surplus should price below balanced stock.");
    AssertTrue(stockout.Scarcity > balanced.Scarcity && balanced.Scarcity > surplus.Scarcity, "Scarcity should follow local stock pressure.");
}

static void PrototypeExposesLocalMarketPressureSignals()
{
    var session = new SimulationBridge().CreatePrototypeSession(424242);
    var snapshot = session.Current;

    AssertTrue(snapshot.Cities.All(city => city.MarketSignals.Count > 0), "Expected every city to expose market pressure signals.");
    AssertTrue(snapshot.Cities.Any(city => city.MarketSignals.Any(signal => signal.WarehouseStock > 0)), "Expected signals to include company warehouse stock.");
    AssertTrue(snapshot.Cities.Any(city => city.MarketSignals.Any(signal => signal.Reason.Contains("short", StringComparison.Ordinal) || signal.Reason.Contains("stockout", StringComparison.Ordinal))), "Expected at least one visible shortage reason.");
}

static void PrototypeWarehousePoliciesExposeSafetyStock()
{
    var snapshot = new SimulationBridge().CreatePrototypeSession(424242).Current;
    var trackedSignals = snapshot.Cities.SelectMany(city => city.MarketSignals).Where(signal => signal.DesiredStock > 0).ToArray();

    AssertTrue(trackedSignals.All(signal => signal.SafetyStock > 0), "Tracked needs should expose positive safety stock.");
    AssertTrue(trackedSignals.All(signal => signal.ReorderPoint >= signal.SafetyStock), "Reorder points should not sit below safety stock.");
    AssertTrue(trackedSignals.All(signal => !signal.IsPolicyOverridden), "Default policy signals should not be marked as overridden.");
    AssertTrue(trackedSignals.Any(signal => signal.ShipmentPriority > 0), "Expected at least one market signal to request shipment priority.");
    AssertTrue(trackedSignals.Any(signal => signal.PolicyAction.Contains("reorder", StringComparison.Ordinal) || signal.PolicyAction.Contains("shipment", StringComparison.Ordinal) || signal.PolicyAction.Contains("top up", StringComparison.Ordinal)), "Expected policy actions to explain shipment decisions.");
}

static void PrototypeWarehousePolicyOverridesAreDeterministic()
{
    var bridge = new SimulationBridge();
    var first = bridge.CreatePrototypeSession(20260429);
    var second = bridge.CreatePrototypeSession(20260429);
    var target = first.Current.Cities[0].MarketSignals.First(signal => signal.DesiredStock > 0);
    var cityId = first.Current.Cities[0].Id;

    AssertTrue(first.SetWarehousePolicy(cityId, target.ResourceId, 9, 17), "Expected first policy override to succeed.");
    AssertTrue(second.SetWarehousePolicy(cityId, target.ResourceId, 9, 17), "Expected second policy override to succeed.");

    for (var i = 0; i < 4; i++)
    {
        first.AdvanceTick();
        second.AdvanceTick();
    }

    AssertEqual(first.Current.SaveHash, second.Current.SaveHash);
    AssertEqual(PolicyFingerprint(first.Current), PolicyFingerprint(second.Current));
    AssertEqual(ContractFingerprint(first.Current.AvailableContracts), ContractFingerprint(second.Current.AvailableContracts));
}

static void PrototypeWarehousePolicyChangesSaveHash()
{
    var session = new SimulationBridge().CreatePrototypeSession(424242);
    var city = session.Current.Cities[0];
    var signal = city.MarketSignals.First(item => item.DesiredStock > 0);
    var initialHash = session.Current.SaveHash;

    AssertTrue(session.SetWarehousePolicy(city.Id, signal.ResourceId, signal.SafetyStock + 1, signal.ReorderPoint + 2), "Expected policy override to succeed.");
    AssertEqual(0, session.Current.Tick);
    AssertTrue(session.Current.SaveHash != initialHash, "Policy overrides should affect the state hash immediately.");

    var updated = SignalFor(session.Current, city.Id, signal.ResourceId);
    AssertTrue(updated.IsPolicyOverridden, "Market signal should expose that the policy is overridden.");
    AssertEqual(signal.SafetyStock + 1, updated.SafetyStock);
    AssertEqual(signal.ReorderPoint + 2, updated.ReorderPoint);
}

static void PrototypeWarehousePolicyClampsAndRejectsInvalidTargets()
{
    var session = new SimulationBridge().CreatePrototypeSession(424242);
    var city = session.Current.Cities[0];
    var signal = city.MarketSignals.First(item => item.DesiredStock > 0);
    var initialHash = session.Current.SaveHash;
    var initialTick = session.Current.Tick;

    AssertTrue(!session.SetWarehousePolicy("missing-city", signal.ResourceId, 2, 4), "Expected unknown city id to be rejected.");
    AssertTrue(!session.SetWarehousePolicy(city.Id, "missing-resource", 2, 4), "Expected unknown resource id to be rejected.");
    AssertTrue(!session.SetWarehousePolicy(city.Id, "wool", 2, 4), "Expected resources without market needs to be rejected as policy targets.");
    AssertEqual(initialTick, session.Current.Tick);
    AssertEqual(initialHash, session.Current.SaveHash);

    AssertTrue(session.SetWarehousePolicy(city.Id, signal.ResourceId, 999, 1), "Expected valid policy target to accept clamped values.");
    var updated = SignalFor(session.Current, city.Id, signal.ResourceId);
    AssertEqual(64, updated.SafetyStock);
    AssertEqual(64, updated.ReorderPoint);
    AssertTrue(updated.IsPolicyOverridden, "Expected clamped policy to be marked as overridden.");
}

static void PrototypeWarehousePolicyAffectsContractPriorityAndAvailability()
{
    var prioritySession = new SimulationBridge().CreatePrototypeSession(424242);
    var priorityCandidate = FindContract(prioritySession.Current, "Expected a contract whose destination priority can be raised by policy.", contract =>
    {
        var signal = SignalFor(prioritySession.Current, contract.ToNode, contract.ResourceId);
        return signal.MarketStock > 0 && signal.ShipmentPriority < 3;
    });
    var oldPriority = priorityCandidate.ShipmentPriority;

    AssertTrue(prioritySession.SetWarehousePolicy(priorityCandidate.ToNode, priorityCandidate.ResourceId, 64, 64), "Expected destination policy override to succeed.");
    var reprioritized = prioritySession.Current.AvailableContracts.Single(contract => contract.Id == priorityCandidate.Id);
    AssertTrue(reprioritized.ShipmentPriority > oldPriority, "Destination policy override should raise route contract shipment priority.");
    AssertTrue(SignalFor(prioritySession.Current, priorityCandidate.ToNode, priorityCandidate.ResourceId).IsPolicyOverridden, "Destination signal should show the override.");

    var availabilitySession = new SimulationBridge().CreatePrototypeSession(424242);
    var availabilityCandidate = FindContract(availabilitySession.Current, "Expected a contract that can be removed by protecting source warehouse stock.", _ => true);
    AssertTrue(availabilitySession.SetWarehousePolicy(availabilityCandidate.FromNode, availabilityCandidate.ResourceId, 64, 64), "Expected source policy override to succeed.");
    AssertTrue(
        availabilitySession.Current.AvailableContracts.All(contract => contract.Id != availabilityCandidate.Id),
        "Source safety stock override should reserve warehouse stock and remove that contract from availability.");
}

static void PrototypeRouteContractsFollowShipmentPriority()
{
    var snapshot = new SimulationBridge().CreatePrototypeSession(424242).Current;
    AssertTrue(snapshot.AvailableContracts.Count > 1, "Expected multiple route contracts to compare.");

    for (var i = 1; i < snapshot.AvailableContracts.Count; i++)
    {
        var previous = snapshot.AvailableContracts[i - 1];
        var current = snapshot.AvailableContracts[i];
        AssertTrue(previous.ShipmentPriority >= current.ShipmentPriority, "Route contracts should be ordered by shipment priority first.");
        AssertTrue(previous.Units > 0 && current.Units > 0, "Route contracts should expose positive reserved units.");
    }

    AssertTrue(snapshot.AvailableContracts.Any(contract => contract.PolicyAction.Length > 0), "Contracts should carry visible policy actions.");
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

static void PrototypeRouteContractsAreDeterministic()
{
    var bridge = new SimulationBridge();
    var first = bridge.CreatePrototypeSession(424242);
    var second = bridge.CreatePrototypeSession(424242);

    AssertTrue(first.Current.AvailableContracts.Count > 0, "Expected starter session to expose route contracts.");
    AssertEqual(
        ContractFingerprint(first.Current.AvailableContracts),
        ContractFingerprint(second.Current.AvailableContracts));
}

static void PrototypeRouteContractSelectionAffectsLogistics()
{
    var bridge = new SimulationBridge();
    var automatic = bridge.CreatePrototypeSession(424242);
    var automaticTick = automatic.AdvanceTick();

    var controlled = bridge.CreatePrototypeSession(424242);
    var contract = controlled.Current.AvailableContracts.Last();
    var unselectedHash = controlled.Current.SaveHash;

    AssertTrue(controlled.SelectRouteContract(contract.Id), "Expected contract selection to succeed.");
    AssertEqual(contract.Id, controlled.Current.SelectedContractId);
    AssertEqual(0, controlled.Current.Tick);
    AssertTrue(controlled.Current.SaveHash != unselectedHash, "Pending contract selection should be represented in the state hash.");

    var controlledTick = controlled.AdvanceTick();
    var controlledLogistics = controlledTick.Ledger
        .Where(entry => entry.Tick == controlledTick.Tick && entry.Category == "Logistics")
        .ToArray();

    AssertEqual(1, controlledLogistics.Length);
    AssertTrue(controlledLogistics[0].RelatedId == contract.RouteId, "Expected selected contract route to drive logistics.");
    AssertTrue(controlledLogistics[0].Message.Contains(contract.ResourceId, StringComparison.Ordinal), "Expected selected contract resource to be delivered.");
    AssertTrue(automaticTick.SaveHash != controlledTick.SaveHash, "Selected contract should change the next logistics result.");
}

static void PrototypeRouteContractRejectsInvalidIds()
{
    var session = new SimulationBridge().CreatePrototypeSession(424242);
    var initialTick = session.Current.Tick;
    var initialHash = session.Current.SaveHash;

    AssertTrue(!session.SelectRouteContract("missing-contract"), "Expected invalid route contract id to be rejected.");
    AssertEqual<string?>(null, session.Current.SelectedContractId);
    AssertEqual(initialTick, session.Current.Tick);
    AssertEqual(initialHash, session.Current.SaveHash);
}

static void PrototypeSelectedRouteContractStaysDeterministic()
{
    var bridge = new SimulationBridge();
    var first = bridge.CreatePrototypeSession(20260429);
    var second = bridge.CreatePrototypeSession(20260429);
    var contractId = first.Current.AvailableContracts.First().Id;

    AssertTrue(first.SelectRouteContract(contractId), "Expected first session contract selection to succeed.");
    AssertTrue(second.SelectRouteContract(contractId), "Expected second session contract selection to succeed.");

    for (var i = 0; i < 4; i++)
    {
        first.AdvanceTick();
        second.AdvanceTick();
    }

    AssertEqual(first.Current.SaveHash, second.Current.SaveHash);
    AssertEqual(first.Current.Company.Cash, second.Current.Company.Cash);
    AssertEqual(ContractFingerprint(first.Current.AvailableContracts), ContractFingerprint(second.Current.AvailableContracts));
}

static void PrototypeRoutePolicyHashIsDeterministic()
{
    var bridge = new SimulationBridge();
    var first = bridge.CreatePrototypeSession(20260429);
    var second = bridge.CreatePrototypeSession(20260429);
    var route = first.Current.AvailableContracts
        .GroupBy(contract => contract.RouteId)
        .First(group => group.Select(contract => contract.ResourceId).Distinct(StringComparer.Ordinal).Count() > 1);
    var removedResource = route.First().ResourceId;
    var priorityResource = route.First(contract => contract.ResourceId != removedResource).ResourceId;

    AssertTrue(first.SetRouteResourceReservation(route.Key, removedResource, false), "Expected first route reservation update to succeed.");
    AssertTrue(second.SetRouteResourceReservation(route.Key, removedResource, false), "Expected second route reservation update to succeed.");
    AssertTrue(first.SetRoutePriorityResource(route.Key, priorityResource), "Expected first route priority update to succeed.");
    AssertTrue(second.SetRoutePriorityResource(route.Key, priorityResource), "Expected second route priority update to succeed.");

    for (var i = 0; i < 4; i++)
    {
        first.AdvanceTick();
        second.AdvanceTick();
    }

    AssertEqual(first.Current.SaveHash, second.Current.SaveHash);
    AssertEqual(RoutePolicyFingerprint(first.Current), RoutePolicyFingerprint(second.Current));
    AssertEqual(ContractFingerprint(first.Current.AvailableContracts), ContractFingerprint(second.Current.AvailableContracts));
}

static void PrototypeRouteResourceReservationFiltersContracts()
{
    var bridge = new SimulationBridge();
    var baseline = bridge.CreatePrototypeSession(20260429);
    var baselineInitial = baseline.Current;
    var baselineTick = baseline.AdvanceTick();
    var baselineDelivery = baselineTick.Ledger
        .Where(entry => entry.Tick == baselineTick.Tick && entry.Category == "Logistics")
        .Select(entry => new
        {
            Entry = entry,
            Contract = baselineInitial.AvailableContracts.FirstOrDefault(contract =>
                contract.RouteId == entry.RelatedId
                && entry.Message.Contains(contract.ResourceId, StringComparison.Ordinal))
        })
        .First(item => item.Contract is not null);
    var routeId = baselineDelivery.Contract!.RouteId;
    var removedResource = baselineDelivery.Contract.ResourceId;

    var session = bridge.CreatePrototypeSession(20260429);
    var initialHash = session.Current.SaveHash;

    AssertTrue(session.SetRouteResourceReservation(routeId, removedResource, false), "Expected route reservation update to accept a valid route/resource.");
    AssertTrue(session.Current.SaveHash != initialHash, "Route reservation should change the deterministic state hash.");
    AssertTrue(session.Current.RoutePolicies.Single(policy => policy.RouteId == routeId).ReservedResources.All(resource => resource != removedResource), "Route policy view should show the resource as unreserved.");
    AssertTrue(session.Current.AvailableContracts.All(contract => contract.RouteId != routeId || contract.ResourceId != removedResource), "Unreserved route resource should be filtered from available contracts.");

    var tick = session.AdvanceTick();
    AssertTrue(!tick.Ledger.Any(entry => entry.Tick == tick.Tick
        && entry.Category == "Logistics"
        && entry.RelatedId == routeId
        && entry.Message.Contains(removedResource, StringComparison.Ordinal)), "Automatic logistics should not ship an unreserved resource on that route.");
}

static void PrototypeRoutePriorityBoostsContractOrdering()
{
    var session = new SimulationBridge().CreatePrototypeSession(20260429);
    var group = session.Current.AvailableContracts
        .GroupBy(contract => contract.RouteId)
        .First(item => item.Select(contract => contract.ResourceId).Distinct(StringComparer.Ordinal).Count() > 1);
    var target = group.Last();
    var oldPriority = target.ShipmentPriority;

    AssertTrue(session.SetRoutePriorityResource(group.Key, target.ResourceId), "Expected route priority update to accept a valid route/resource.");
    var updatedGroup = session.Current.AvailableContracts.Where(contract => contract.RouteId == group.Key).ToArray();
    var updatedTarget = updatedGroup.Single(contract => contract.ResourceId == target.ResourceId);

    AssertEqual(target.ResourceId, updatedGroup[0].ResourceId);
    AssertTrue(updatedTarget.ShipmentPriority > oldPriority, "Route priority should boost the target contract.");
    AssertTrue(updatedTarget.PolicyAction.Contains("route priority", StringComparison.Ordinal), "Prioritized contract should expose route priority in its action text.");
}

static void PrototypeRoutePolicyInvalidTargetsAreNoOps()
{
    var session = new SimulationBridge().CreatePrototypeSession(424242);
    var routeId = session.Current.Routes[0].Id;
    var resourceId = session.Current.RoutePolicies[0].ReservedResources[0];
    var initialTick = session.Current.Tick;
    var initialHash = session.Current.SaveHash;
    var initialPolicies = RoutePolicyFingerprint(session.Current);

    AssertTrue(!session.SetRouteResourceReservation("missing-route", resourceId, false), "Expected unknown route id to be rejected.");
    AssertTrue(!session.SetRouteResourceReservation(routeId, "missing-resource", false), "Expected unknown resource id to be rejected.");
    AssertTrue(!session.SetRoutePriorityResource("missing-route", resourceId), "Expected unknown route priority target to be rejected.");
    AssertTrue(!session.SetRoutePriorityResource(routeId, "missing-resource"), "Expected unknown priority resource to be rejected.");

    AssertEqual(initialTick, session.Current.Tick);
    AssertEqual(initialHash, session.Current.SaveHash);
    AssertEqual(initialPolicies, RoutePolicyFingerprint(session.Current));
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
    var save = StarterSaveFactory.Create(424242, snapshot.World.WorldGenVersion, content.ContentHash, snapshot.World.Nodes, routes, content.Resources, market, prices);
    var firstHash = SaveCodec.ComputeStateHash(save);
    var json = SaveCodec.Serialize(save);
    var loaded = SaveCodec.Deserialize(json);
    var secondJson = SaveCodec.Serialize(loaded);
    var secondHash = SaveCodec.ComputeStateHash(SaveCodec.Deserialize(secondJson));
    AssertEqual(firstHash, secondHash);
}

static void WarehousePolicySaveLoadPreservesHash()
{
    var content = GameContentLoader.LoadFromDirectory(ContentPathResolver.FindContentDirectory());
    var snapshot = new SimulationBridge().CreateNewGame(424242);
    var routes = RoutePlanner.FromWorld(snapshot.World);
    var market = StarterScenarioFactory.CreateInitialMarket(content.Resources);
    var prices = new EconomyTick().CalculatePrices(content.Resources, market, StarterScenarioFactory.CreateNeeds(content.Resources));
    var save = StarterSaveFactory.Create(424242, snapshot.World.WorldGenVersion, content.ContentHash, snapshot.World.Nodes, routes, content.Resources, market, prices) with
    {
        WarehousePolicies =
        [
            new WarehousePolicySaveState(snapshot.World.Nodes[0].Id, "grain", 5, 12)
        ]
    };
    var firstHash = SaveCodec.ComputeStateHash(save);
    var json = SaveCodec.Serialize(save);
    var loaded = SaveCodec.Deserialize(json);
    var secondHash = SaveCodec.ComputeStateHash(loaded);

    AssertEqual(firstHash, secondHash);
    AssertTrue(firstHash != SaveCodec.ComputeStateHash(save with { WarehousePolicies = [] }), "Warehouse policy saves should affect state hash.");

    try
    {
        SaveCodec.ComputeStateHash(save with { SaveVersion = 1 });
        throw new InvalidOperationException("Expected save validation to reject an old save version without migration.");
    }
    catch (SaveValidationException ex)
    {
        AssertTrue(ex.Errors.Any(error => error.Contains("saveVersion", StringComparison.Ordinal)), "Expected save version validation error.");
    }
}

static void RoutePolicySaveValidationRejectsOrphanPriority()
{
    var content = GameContentLoader.LoadFromDirectory(ContentPathResolver.FindContentDirectory());
    var snapshot = new SimulationBridge().CreateNewGame(424242);
    var routes = RoutePlanner.FromWorld(snapshot.World);
    var market = StarterScenarioFactory.CreateInitialMarket(content.Resources);
    var prices = new EconomyTick().CalculatePrices(content.Resources, market, StarterScenarioFactory.CreateNeeds(content.Resources));
    var save = StarterSaveFactory.Create(424242, snapshot.World.WorldGenVersion, content.ContentHash, snapshot.World.Nodes, routes, content.Resources, market, prices);
    var invalid = save with
    {
        RoutePolicies =
        [
            new RoutePolicySaveState(save.Routes[0].Id, [], "grain")
        ]
    };

    try
    {
        SaveCodec.Serialize(invalid);
        throw new InvalidOperationException("Expected save validation to reject an orphan route priority.");
    }
    catch (SaveValidationException ex)
    {
        AssertTrue(ex.Errors.Any(error => error.Contains("priorityResourceId must be one of reservedResources", StringComparison.Ordinal)), "Expected orphan route priority validation error.");
    }

    var missingPolicy = save with { RoutePolicies = save.RoutePolicies.Skip(1).ToArray() };
    try
    {
        SaveCodec.Serialize(missingPolicy);
        throw new InvalidOperationException("Expected save validation to reject missing route policy state.");
    }
    catch (SaveValidationException ex)
    {
        AssertTrue(ex.Errors.Any(error => error.Contains("must be present for every saved route", StringComparison.Ordinal)), "Expected missing route policy validation error.");
    }

    var unknownResource = save with
    {
        RoutePolicies =
        [
            save.RoutePolicies[0] with { ReservedResources = ["not-a-route-resource"] },
            .. save.RoutePolicies.Skip(1)
        ]
    };
    try
    {
        SaveCodec.Serialize(unknownResource);
        throw new InvalidOperationException("Expected save validation to reject unknown route policy resources.");
    }
    catch (SaveValidationException ex)
    {
        AssertTrue(ex.Errors.Any(error => error.Contains("must be listed in the saved route reservedFor resources", StringComparison.Ordinal)), "Expected unknown route policy resource validation error.");
    }
}

static void SaveLoadRejectsNegativeStock()
{
    var content = GameContentLoader.LoadFromDirectory(ContentPathResolver.FindContentDirectory());
    var snapshot = new SimulationBridge().CreateNewGame(424242);
    var routes = RoutePlanner.FromWorld(snapshot.World);
    var market = StarterScenarioFactory.CreateInitialMarket(content.Resources);
    var prices = new EconomyTick().CalculatePrices(content.Resources, market, StarterScenarioFactory.CreateNeeds(content.Resources));
    var save = StarterSaveFactory.Create(424242, snapshot.World.WorldGenVersion, content.ContentHash, snapshot.World.Nodes, routes, content.Resources, market, prices);
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

static bool TouchesWater(GeneratedWorld world, TerrainCell cell)
{
    return TouchesWaterAt(world, cell.X, cell.Y);
}

static bool TouchesWaterAt(GeneratedWorld world, int x, int y)
{
    return IsWater(world, x - 1, y)
        || IsWater(world, x + 1, y)
        || IsWater(world, x, y - 1)
        || IsWater(world, x, y + 1);
}

static bool IsWater(GeneratedWorld world, int x, int y)
{
    return world.Terrain.FirstOrDefault(cell => cell.X == x && cell.Y == y)?.IsWater ?? true;
}

static double NodeDistance(WorldNode left, WorldNode right)
{
    var dx = left.X - right.X;
    var dy = left.Y - right.Y;
    return Math.Sqrt(dx * dx + dy * dy);
}

static string ContractFingerprint(IEnumerable<PrototypeRouteContractView> contracts)
{
    return string.Join("|", contracts.Select(contract =>
        string.Format(
            CultureInfo.InvariantCulture,
            "{0}:{1}:{2}->{3}:{4}:{5:0.00}:{6:0.00}:{7:0.00}:{8}:{9}:{10}:{11}",
            contract.Id,
            contract.RouteId,
            contract.FromNode,
            contract.ToNode,
            contract.ResourceId,
            contract.ExpectedRevenue,
            contract.TransportCost,
            contract.ExpectedNet,
            contract.CapacityPerDay,
            contract.Units,
            contract.ShipmentPriority,
            contract.PolicyAction)));
}

static PrototypeMarketSignal SignalFor(PrototypeSnapshot snapshot, string cityId, string resourceId)
{
    return snapshot.Cities
        .Single(city => city.Id == cityId)
        .MarketSignals
        .Single(signal => signal.ResourceId == resourceId);
}

static PrototypeRouteContractView FindContract(PrototypeSnapshot snapshot, string failureMessage, Func<PrototypeRouteContractView, bool> predicate)
{
    var contract = snapshot.AvailableContracts.FirstOrDefault(predicate);
    AssertTrue(contract is not null, $"{failureMessage} Available: {ContractFingerprint(snapshot.AvailableContracts)}");
    return contract!;
}

static string PolicyFingerprint(PrototypeSnapshot snapshot)
{
    return string.Join("|", snapshot.Cities
        .OrderBy(city => city.Id, StringComparer.Ordinal)
        .SelectMany(city => city.MarketSignals.Select(signal =>
            string.Format(
                CultureInfo.InvariantCulture,
                "{0}:{1}:{2}:{3}:{4}:{5}",
                city.Id,
                signal.ResourceId,
                signal.SafetyStock,
                signal.ReorderPoint,
                signal.IsPolicyOverridden,
                signal.ShipmentPriority))));
}

static string RoutePolicyFingerprint(PrototypeSnapshot snapshot)
{
    return string.Join("|", snapshot.RoutePolicies
        .OrderBy(policy => policy.RouteId, StringComparer.Ordinal)
        .Select(policy =>
            string.Format(
                CultureInfo.InvariantCulture,
                "{0}:{1}:{2}",
                policy.RouteId,
                string.Join(",", policy.ReservedResources.Order(StringComparer.Ordinal)),
                policy.PriorityResourceId ?? "")));
}
