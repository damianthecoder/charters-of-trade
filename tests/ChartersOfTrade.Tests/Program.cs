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
    ("prototype city specializations are deterministic", PrototypeCitySpecializationsAreDeterministic),
    ("prototype production chain opportunities are deterministic", PrototypeProductionChainOpportunitiesAreDeterministic),
    ("prototype production chain opportunities explain inputs and outputs", PrototypeProductionChainOpportunitiesExplainInputsAndOutputs),
    ("prototype production chain opportunities respect warehouse reserve", PrototypeProductionChainOpportunitiesRespectWarehouseReserve),
    ("prototype production focus changes save hash", PrototypeProductionFocusChangesSaveHash),
    ("prototype production policy invalid targets are no-ops", PrototypeProductionPolicyInvalidTargetsAreNoOps),
    ("prototype session ticks deterministically", PrototypeSessionTicksDeterministically),
    ("prototype session advances all systems", PrototypeSessionAdvancesAllSystems),
    ("prototype route contracts are deterministic", PrototypeRouteContractsAreDeterministic),
    ("prototype route contract selection affects logistics", PrototypeRouteContractSelectionAffectsLogistics),
    ("prototype route contract rejects invalid ids", PrototypeRouteContractRejectsInvalidIds),
    ("prototype selected route contract stays deterministic", PrototypeSelectedRouteContractStaysDeterministic),
    ("prototype route operations are deterministic", PrototypeRouteOperationsAreDeterministic),
    ("prototype route operation selection exposes active state", PrototypeRouteOperationSelectionExposesActiveState),
    ("prototype route operations support active network", PrototypeRouteOperationsSupportActiveNetwork),
    ("prototype route operations create transit queue", PrototypeRouteOperationsCreateTransitQueue),
    ("prototype route throughput metrics are deterministic", PrototypeRouteThroughputMetricsAreDeterministic),
    ("prototype route operation stop prevents selected objective credit", PrototypeRouteOperationStopPreventsSelectedObjectiveCredit),
    ("prototype route operation pauses when cargo is blocked", PrototypeRouteOperationPausesWhenCargoIsBlocked),
    ("prototype scenario objective is deterministic", PrototypeScenarioObjectiveIsDeterministic),
    ("prototype scenario objective counts selected deliveries", PrototypeScenarioObjectiveCountsSelectedDeliveries),
    ("first charter season rules can be won", FirstCharterSeasonRulesCanBeWon),
    ("scripted first charter season can win a benchmark seed", ScriptedFirstCharterSeasonCanWinABenchmarkSeed),
    ("prototype scenario objective stability ignores lowered policy", PrototypeScenarioObjectiveStabilityIgnoresLoweredPolicy),
    ("prototype scenario objective times out without charters", PrototypeScenarioObjectiveTimesOutWithoutCharters),
    ("prototype NPC pressure is deterministic", PrototypeNpcPressureIsDeterministic),
    ("prototype NPC pressure ordering is stable", PrototypeNpcPressureOrderingIsStable),
    ("NPC pressure scorer tie-breaks and blocks safely", NpcPressureScorerTieBreaksAndBlocksSafely),
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
    ("prototype warehouse automation modes affect policy and hash", PrototypeWarehouseAutomationModesAffectPolicyAndHash),
    ("prototype warehouse policy clamps and rejects invalid targets", PrototypeWarehousePolicyClampsAndRejectsInvalidTargets),
    ("prototype warehouse policy affects contract priority and availability", PrototypeWarehousePolicyAffectsContractPriorityAndAvailability),
    ("prototype route contracts follow shipment priority", PrototypeRouteContractsFollowShipmentPriority),
    ("economy production never creates negative stock", EconomyProductionNeverCreatesNegativeStock),
    ("save-load-save preserves hash", SaveLoadSavePreservesHash),
    ("warehouse policy save-load preserves hash", WarehousePolicySaveLoadPreservesHash),
    ("production policy save-load preserves hash", ProductionPolicySaveLoadPreservesHash),
    ("production policy validation rejects invalid state", ProductionPolicyValidationRejectsInvalidState),
    ("scenario objective save-load preserves hash", ScenarioObjectiveSaveLoadPreservesHash),
    ("route operation save-load preserves hash", RouteOperationSaveLoadPreservesHash),
    ("route transit validation rejects invalid state", RouteTransitValidationRejectsInvalidState),
    ("scenario objective validation rejects invalid state", ScenarioObjectiveValidationRejectsInvalidState),
    ("route policy save validation rejects orphan priority", RoutePolicySaveValidationRejectsOrphanPriority),
    ("save load rejects negative stock", SaveLoadRejectsNegativeStock),
    ("simulation core projects remain Godot-free", SimulationCoreProjectsRemainGodotFree),
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
    var session = new SimulationBridge().CreatePrototypeSession(20260429);
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
    AssertTrue(trackedSignals.All(signal => signal.PolicyMode == PrototypeSession.BalancedWarehouseMode), "Default policy signals should start in balanced mode.");
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
    var session = new SimulationBridge().CreatePrototypeSession(20260429);
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

static void PrototypeWarehouseAutomationModesAffectPolicyAndHash()
{
    var session = new SimulationBridge().CreatePrototypeSession(424242);
    var city = session.Current.Cities[0];
    var signal = city.MarketSignals.First(item => item.DesiredStock > 0);
    var initialHash = session.Current.SaveHash;

    AssertEqual(PrototypeSession.BalancedWarehouseMode, signal.PolicyMode);
    AssertTrue(session.SetWarehousePolicyMode(city.Id, signal.ResourceId, PrototypeSession.ConservativeWarehouseMode), "Expected conservative warehouse mode to apply.");
    var conservative = SignalFor(session.Current, city.Id, signal.ResourceId);

    AssertTrue(session.Current.SaveHash != initialHash, "Non-default warehouse mode should affect the state hash.");
    AssertTrue(conservative.IsPolicyOverridden, "Conservative mode should be stored as an explicit policy override.");
    AssertEqual(PrototypeSession.ConservativeWarehouseMode, conservative.PolicyMode);
    AssertTrue(conservative.SafetyStock >= signal.SafetyStock, "Conservative mode should not lower safety stock.");
    AssertTrue(conservative.ReorderPoint > signal.ReorderPoint || conservative.SafetyStock > signal.SafetyStock, "Conservative mode should raise at least one policy threshold.");
    AssertTrue(conservative.PolicyAction.Contains("conservative", StringComparison.Ordinal), "Policy action should expose conservative mode.");

    var contractSession = new SimulationBridge().CreatePrototypeSession(424242);
    var contractCandidate = contractSession.Current.AvailableContracts.First();
    AssertTrue(contractSession.SetWarehousePolicyMode(contractCandidate.ToNode, contractCandidate.ResourceId, PrototypeSession.ConservativeWarehouseMode), "Expected Conservative mode to apply to a contract destination.");
    var updatedContract = contractSession.Current.AvailableContracts.Single(contract => contract.Id == contractCandidate.Id);
    AssertTrue(updatedContract.PolicyAction.Contains("conservative", StringComparison.Ordinal), "Contract action should expose conservative mode.");
    AssertTrue(updatedContract.PolicyAction != contractCandidate.PolicyAction, "Contract policy text should change when Conservative mode is applied.");

    AssertTrue(session.SetWarehousePolicyMode(city.Id, signal.ResourceId, PrototypeSession.BalancedWarehouseMode), "Expected balanced warehouse mode to reset policy.");
    var balanced = SignalFor(session.Current, city.Id, signal.ResourceId);
    AssertEqual(PrototypeSession.BalancedWarehouseMode, balanced.PolicyMode);
    AssertTrue(!balanced.IsPolicyOverridden, "Balanced mode should return to default policy state.");
    AssertEqual(signal.SafetyStock, balanced.SafetyStock);
    AssertEqual(signal.ReorderPoint, balanced.ReorderPoint);
    AssertEqual(initialHash, session.Current.SaveHash);
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
    AssertTrue(!session.SetWarehousePolicyMode(city.Id, signal.ResourceId, "reckless"), "Expected unknown warehouse mode to be rejected.");
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

static void PrototypeCitySpecializationsAreDeterministic()
{
    var bridge = new SimulationBridge();
    var first = bridge.CreatePrototypeSession(20260429);
    var second = bridge.CreatePrototypeSession(20260429);
    var initialFingerprint = CitySpecializationFingerprint(first.Current);

    AssertEqual(initialFingerprint, CitySpecializationFingerprint(second.Current));
    AssertTrue(first.Current.Cities.All(city => city.Districts.Contains("market", StringComparer.Ordinal)), "Every prototype city should expose its market district.");
    AssertTrue(first.Current.Cities.All(city => !string.IsNullOrWhiteSpace(city.Specialization.RoleId)), "Every prototype city should expose a specialization role id.");
    AssertTrue(first.Current.Cities.Select(city => city.Specialization.RoleId).Distinct(StringComparer.Ordinal).Count() >= 3, "Expected seed 20260429 to expose at least three city roles.");
    AssertTrue(first.Current.Cities.All(city => IsReadOnlySnapshotList(city.Districts)), "City districts should be exposed as read-only snapshot lists.");
    AssertTrue(
        first.Current.Cities.All(city =>
            IsReadOnlySnapshotList(city.Specialization.AnchorResources) &&
            IsReadOnlySnapshotList(city.Specialization.OutputResources)),
        "City specialization resources should be exposed as read-only snapshot lists.");

    foreach (var city in first.Current.Cities)
    {
        var node = first.Current.World.Nodes.Single(node => node.Id == city.Id);
        AssertTrue(
            city.Specialization.AnchorResources.All(resource => node.Resources.Contains(resource, StringComparer.Ordinal)),
            $"Expected {city.Id} specialization anchors to come from its world-node resources.");
    }

    for (var i = 0; i < 3; i++)
    {
        first.AdvanceTick();
    }

    AssertEqual(initialFingerprint, CitySpecializationFingerprint(first.Current));
}

static bool IsReadOnlySnapshotList(IReadOnlyList<string> values)
{
    return values is not string[] &&
        (values is not ICollection<string> collection || collection.IsReadOnly);
}

static bool IsReadOnlySnapshotObjectList<T>(IReadOnlyList<T> values)
{
    return values is not T[] &&
        (values is not ICollection<T> collection || collection.IsReadOnly);
}

static void PrototypeProductionChainOpportunitiesAreDeterministic()
{
    var bridge = new SimulationBridge();
    var first = bridge.CreatePrototypeSession(20260429);
    var second = bridge.CreatePrototypeSession(20260429);
    var initialFingerprint = ProductionChainFingerprint(first.Current.ProductionChainOpportunities);

    AssertTrue(first.Current.ProductionChainOpportunities.Count > 0, "Expected starter session to expose production chain opportunities.");
    AssertEqual(initialFingerprint, ProductionChainFingerprint(second.Current.ProductionChainOpportunities));
    AssertTrue(IsReadOnlySnapshotObjectList(first.Current.ProductionChainOpportunities), "Production opportunities should be exposed as read-only snapshot lists.");
    AssertTrue(
        first.Current.ProductionChainOpportunities.All(opportunity =>
            IsReadOnlySnapshotObjectList(opportunity.Inputs) &&
            IsReadOnlySnapshotObjectList(opportunity.Outputs)),
        "Production opportunity resource lines should be exposed as read-only snapshot lists.");

    var lastReadyIndex = LastIndex(first.Current.ProductionChainOpportunities, opportunity => opportunity.IsReady);
    var firstBlockedIndex = FirstIndex(first.Current.ProductionChainOpportunities, opportunity => !opportunity.IsReady);
    if (lastReadyIndex >= 0 && firstBlockedIndex >= 0)
    {
        AssertTrue(lastReadyIndex < firstBlockedIndex, "Ready production chains should sort before blocked chains.");
    }

    for (var i = 0; i < 3; i++)
    {
        first.AdvanceTick();
        second.AdvanceTick();
    }

    AssertEqual(
        ProductionChainFingerprint(first.Current.ProductionChainOpportunities),
        ProductionChainFingerprint(second.Current.ProductionChainOpportunities));
}

static void PrototypeProductionChainOpportunitiesExplainInputsAndOutputs()
{
    var snapshot = new SimulationBridge().CreatePrototypeSession(424242).Current;
    var chain = snapshot.ProductionChainOpportunities.FirstOrDefault(opportunity =>
        opportunity.Inputs.Count > 0 &&
        opportunity.Outputs.Count > 0);

    AssertTrue(chain is not null, "Expected at least one production chain with inputs and outputs.");
    AssertTrue(chain!.InputCost > 0, "Production chain should expose input replacement cost.");
    AssertTrue(chain.OutputValue > 0, "Production chain should expose output value.");
    AssertTrue(!string.IsNullOrWhiteSpace(chain.Reason), "Production chain should expose a short reason.");
    AssertTrue(chain.Inputs.All(input => input.RequiredAmount > 0 && input.LocalUnitPrice > 0), "Production inputs should expose quantities and prices.");
    AssertTrue(chain.Outputs.All(output => output.OutputAmount > 0 && output.LocalUnitPrice > 0), "Production outputs should expose quantities and prices.");
    AssertTrue(
        snapshot.ProductionChainOpportunities.Any(opportunity => opportunity.Outputs.Any(output =>
            output.BestDestinationCityId is not null &&
            output.BestRouteId is not null)),
        "Expected at least one production chain output to expose destination demand.");
}

static void PrototypeProductionChainOpportunitiesRespectWarehouseReserve()
{
    var session = new SimulationBridge().CreatePrototypeSession(424242);
    var target = session.Current.ProductionChainOpportunities.FirstOrDefault(opportunity =>
        opportunity.IsReady &&
        opportunity.Inputs.Any(input => input.RequiredAmount > 0 &&
            session.Current.Cities
                .First(city => city.Id == opportunity.CityId)
                .MarketSignals.Any(signal => signal.ResourceId == input.ResourceId && signal.DesiredStock > 0)));

    AssertTrue(target is not null, "Expected at least one ready production chain with a policy-tracked input.");
    var targetInput = target!.Inputs.First(input => input.RequiredAmount > 0 &&
        session.Current.Cities
            .First(city => city.Id == target.CityId)
            .MarketSignals.Any(signal => signal.ResourceId == input.ResourceId && signal.DesiredStock > 0));
    var previousHash = session.Current.SaveHash;

    AssertTrue(
        session.SetWarehousePolicy(target.CityId, targetInput.ResourceId, targetInput.WarehouseStock, targetInput.WarehouseStock),
        "Expected warehouse policy override for production input to succeed.");
    AssertTrue(session.Current.SaveHash != previousHash, "Warehouse reserve policy should remain gameplay state.");

    var updated = session.Current.ProductionChainOpportunities.Single(opportunity => opportunity.Id == target.Id);
    var updatedInput = updated.Inputs.Single(input => input.ResourceId == targetInput.ResourceId);
    AssertTrue(!updated.IsReady, "High safety stock should block the formerly ready chain.");
    AssertEqual(targetInput.WarehouseStock, updatedInput.ProtectedStock);
    AssertEqual(0, updatedInput.AvailableAmount);
    AssertTrue(updatedInput.MissingAmount >= targetInput.RequiredAmount, "Protected stock should be reported as missing input.");
    AssertTrue(updated.Reason.Contains("protected", StringComparison.Ordinal), "Production reason should explain protected stock.");

    var beforeTickWarehouse = session.Current.Cities
        .Single(city => city.Id == target.CityId)
        .CompanyWarehouse[targetInput.ResourceId];
    var tick = session.AdvanceTick();
    var afterTickWarehouse = tick.Cities
        .Single(city => city.Id == target.CityId)
        .CompanyWarehouse[targetInput.ResourceId];
    AssertEqual(beforeTickWarehouse, afterTickWarehouse);
}

static void PrototypeProductionFocusChangesSaveHash()
{
    var session = new SimulationBridge().CreatePrototypeSession(424242);
    var chain = session.Current.ProductionChainOpportunities.First(opportunity => opportunity.IsReady);
    var initialHash = session.Current.SaveHash;

    AssertTrue(session.SetProductionFocus(chain.CityId, chain.RecipeId), "Expected production focus to accept a known city and recipe.");
    AssertEqual(0, session.Current.Tick);
    AssertTrue(session.Current.SaveHash != initialHash, "Production focus should affect the state hash immediately.");
    var policy = session.Current.ProductionPolicies.Single(item => item.CityId == chain.CityId);
    AssertEqual(PrototypeSession.FocusProductionMode, policy.Mode);
    AssertEqual(chain.RecipeId, policy.FocusRecipeId);
    AssertTrue(policy.Summary.Contains(chain.RecipeId, StringComparison.Ordinal), "Production policy summary should name the focused recipe.");

    var focusedHash = session.Current.SaveHash;
    var tick = session.AdvanceTick();
    AssertTrue(tick.Ledger.Any(entry => entry.Tick == tick.Tick
        && entry.Category == "Production"
        && entry.RelatedId == chain.CityId
        && entry.Message.Contains($"focus {chain.RecipeId}", StringComparison.Ordinal)), "Focused production should be visible in the production ledger.");
    AssertEqual(PrototypeSession.FocusProductionMode, tick.ProductionPolicies.Single(item => item.CityId == chain.CityId).Mode);

    AssertTrue(session.ClearProductionFocus(chain.CityId), "Expected production focus to be clearable.");
    var cleared = session.Current.ProductionPolicies.Single(item => item.CityId == chain.CityId);
    AssertEqual(PrototypeSession.AutoProductionMode, cleared.Mode);
    AssertEqual<string?>(null, cleared.FocusRecipeId);
    AssertTrue(session.Current.SaveHash != focusedHash, "Clearing production focus should change the state hash.");

    var paused = new SimulationBridge().CreatePrototypeSession(424242);
    AssertTrue(paused.PauseProduction(chain.CityId), "Expected production pause to accept a known city.");
    AssertEqual(PrototypeSession.PausedProductionMode, paused.Current.ProductionPolicies.Single(item => item.CityId == chain.CityId).Mode);
    var pausedTick = paused.AdvanceTick();
    AssertTrue(pausedTick.Ledger.Any(entry => entry.Tick == pausedTick.Tick
        && entry.Category == "Production"
        && entry.RelatedId == chain.CityId
        && entry.Message.Contains("production paused", StringComparison.Ordinal)), "Paused production should be visible in the production ledger.");
}

static void PrototypeProductionPolicyInvalidTargetsAreNoOps()
{
    var session = new SimulationBridge().CreatePrototypeSession(424242);
    var chain = session.Current.ProductionChainOpportunities.First();
    var initialTick = session.Current.Tick;
    var initialHash = session.Current.SaveHash;
    var initialPolicies = ProductionPolicyFingerprint(session.Current);

    AssertTrue(!session.SetProductionFocus("missing-city", chain.RecipeId), "Expected unknown city to be rejected.");
    AssertTrue(!session.SetProductionFocus(chain.CityId, "missing-recipe"), "Expected unknown recipe to be rejected.");
    AssertTrue(!session.ClearProductionFocus("missing-city"), "Expected unknown city clear to be rejected.");
    AssertTrue(!session.PauseProduction("missing-city"), "Expected unknown city pause to be rejected.");

    AssertEqual(initialTick, session.Current.Tick);
    AssertEqual(initialHash, session.Current.SaveHash);
    AssertEqual(initialPolicies, ProductionPolicyFingerprint(session.Current));
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
    var controlled = bridge.CreatePrototypeSession(20260429);
    var contract = FindContract(controlled.Current, "Expected seed 20260429 to expose a dispatchable wood route operation.", candidate => candidate.ResourceId == "wood" && candidate.ExpectedNet > 0m);
    var unselectedHash = controlled.Current.SaveHash;
    var automatic = bridge.CreatePrototypeSession(20260429);

    AssertTrue(controlled.SelectRouteContract(contract.Id), "Expected contract selection to succeed.");
    AssertEqual(contract.Id, controlled.Current.SelectedContractId);
    AssertTrue(controlled.Current.SaveHash != unselectedHash, "Pending contract selection should be represented in the state hash.");

    var dispatch = AdvanceUntilRouteOperationDelivery(controlled, contract.RouteId, contract.ResourceId);
    while (automatic.Current.Tick < dispatch.Snapshot.Tick)
    {
        automatic.AdvanceTick();
    }

    AssertTrue(dispatch.Delivery.CashDelta > 0m, "Selected route operation should produce positive logistics cash when it dispatches.");
    AssertTrue(automatic.Current.SaveHash != dispatch.Snapshot.SaveHash, "Selected contract should change the next logistics result.");
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

static void PrototypeRouteOperationsAreDeterministic()
{
    var bridge = new SimulationBridge();
    var first = bridge.CreatePrototypeSession(424242);
    var second = bridge.CreatePrototypeSession(424242);

    AssertTrue(first.Current.RouteOperationCandidates.Count > 0, "Expected starter session to expose route operation candidates.");
    AssertEqual(
        RouteOperationFingerprint(first.Current.RouteOperationCandidates),
        RouteOperationFingerprint(second.Current.RouteOperationCandidates));

    for (var i = 1; i < first.Current.RouteOperationCandidates.Count; i++)
    {
        var previous = first.Current.RouteOperationCandidates[i - 1];
        var current = first.Current.RouteOperationCandidates[i];
        AssertTrue(previous.ShipmentPriority >= current.ShipmentPriority, "Route operations should sort by shipment priority first.");
        if (previous.ShipmentPriority == current.ShipmentPriority)
        {
            AssertTrue(previous.ExpectedNet >= current.ExpectedNet, "Route operations should break priority ties by expected net.");
            if (previous.ExpectedNet == current.ExpectedNet)
            {
                AssertTrue(string.CompareOrdinal(previous.Id, current.Id) <= 0, "Route operations should break net ties by id.");
            }
        }
    }
}

static void PrototypeRouteOperationSelectionExposesActiveState()
{
    var session = new SimulationBridge().CreatePrototypeSession(20260429);
    var contract = FindContract(session.Current, "Expected seed 20260429 to expose a dispatchable wood route operation.", candidate => candidate.ResourceId == "wood" && candidate.ExpectedNet > 0m);
    var initialHash = session.Current.SaveHash;

    AssertTrue(session.SelectRouteContract(contract.Id), "Expected route contract selection to start an operation.");
    var active = session.Current.ActiveRouteOperation;
    AssertTrue(active is not null, "Expected selected route contract to expose an active route operation.");
    AssertEqual(contract.Id, active!.SourceContractId);
    AssertEqual(contract.RouteId, active.RouteId);
    AssertEqual(contract.ResourceId, active.ResourceId);
    AssertTrue(active.IsActive, "Selected operation should be marked active.");
    AssertTrue(active.CanDispatch || active.PausedReason.Length > 0, "Active operation should explain readiness or pause state.");
    AssertTrue(active.CapacityPerDay > 0, "Active operation should expose route capacity.");
    AssertTrue(active.UnmetDemandServed <= active.ExpectedUnits, "Unmet demand served should not exceed dispatched units.");
    AssertTrue(session.Current.SaveHash != initialHash, "Active route operation should affect the state hash through pending route contract state.");

    var dispatch = AdvanceUntilRouteOperationDelivery(session, contract.RouteId, contract.ResourceId);
    var tick = dispatch.Snapshot;
    AssertEqual(contract.Id, tick.SelectedContractId);
    AssertTrue(tick.ActiveRouteOperation is not null, "Active route operation should remain recurring after a tick.");
    AssertTrue(dispatch.Delivery.Message.Contains("route operation delivered", StringComparison.Ordinal), "Selected route operation should drive an actual delivery ledger entry.");
    AssertTrue(dispatch.Delivery.CashDelta > 0m, "Selected route operation delivery should contribute positive route cash.");

    var hashWithOperation = tick.SaveHash;
    AssertTrue(session.ClearRouteOperation(), "Expected active route operation to be clearable.");
    AssertEqual<string?>(null, session.Current.SelectedContractId);
    AssertTrue(session.Current.ActiveRouteOperation is null, "Clearing the operation should remove the active operation view.");
    AssertTrue(session.Current.SaveHash != hashWithOperation, "Clearing the operation should change the state hash.");
}

static void PrototypeRouteOperationsSupportActiveNetwork()
{
    var session = new SimulationBridge().CreatePrototypeSession(424242);
    var contracts = session.Current.AvailableContracts
        .Where(contract => contract.ExpectedNet > 0m)
        .GroupBy(contract => contract.RouteId, StringComparer.Ordinal)
        .Select(group => group.OrderByDescending(contract => contract.ShipmentPriority).ThenByDescending(contract => contract.ExpectedNet).First())
        .Take(3)
        .ToArray();

    AssertTrue(contracts.Length >= 2, "Expected seed 424242 to expose contracts on at least two routes.");
    var initialHash = session.Current.SaveHash;
    foreach (var contract in contracts)
    {
        AssertTrue(session.SelectRouteContract(contract.Id), $"Expected contract {contract.Id} to activate.");
    }

    AssertEqual(contracts[^1].Id, session.Current.SelectedContractId);
    AssertTrue(session.Current.ActiveRouteOperations.Count >= 2, "Expected multiple active route operations.");
    AssertTrue(session.Current.SaveHash != initialHash, "The active route-operation network should affect the state hash.");
    foreach (var contract in contracts)
    {
        AssertTrue(
            session.Current.ActiveRouteOperations.Any(operation => operation.SourceContractId == contract.Id),
            $"Expected active operation for {contract.Id}.");
    }

    var routeIds = session.Current.ActiveRouteOperations.Select(operation => operation.RouteId).Distinct(StringComparer.Ordinal).ToArray();
    AssertTrue(routeIds.Length >= 2, "Expected active operations to span multiple routes.");
    AssertTrue(session.ClearRouteOperation(routeIds[0]), "Expected route-scoped stop to remove one active operation.");
    AssertTrue(session.Current.ActiveRouteOperations.All(operation => operation.RouteId != routeIds[0]), "Expected only the selected route operation to stop.");
    AssertTrue(session.Current.ActiveRouteOperations.Count >= 1, "Stopping one route should leave the rest of the network active.");
}

static void PrototypeRouteOperationsCreateTransitQueue()
{
    var session = new SimulationBridge().CreatePrototypeSession(20260429);
    var contract = FindContract(session.Current, "Expected seed 20260429 to expose a profitable wood route operation.", candidate => candidate.ResourceId == "wood" && candidate.ExpectedNet > 0m);

    AssertTrue(session.SelectRouteContract(contract.Id), "Expected route contract selection to start an operation.");
    var dispatched = session.AdvanceTick();
    AssertTrue(dispatched.RouteTransits.Any(transit => transit.OperationId == dispatched.ActiveRouteOperation?.Id), "Expected dispatch to create in-transit cargo.");
    AssertTrue(dispatched.Ledger.Any(entry => entry.Tick == dispatched.Tick
        && entry.Category == "Logistics"
        && entry.RelatedId == contract.RouteId
        && entry.Message.Contains("dispatched", StringComparison.Ordinal)
        && entry.Message.Contains("arrives tick", StringComparison.Ordinal)), "Expected dispatch ledger entry to explain transit timing.");

    var delivery = AdvanceUntilRouteOperationDelivery(session, contract.RouteId, contract.ResourceId, maxTicks: 8);
    AssertTrue(delivery.Snapshot.RouteTransits.All(transit => transit.ArrivalTick > delivery.Snapshot.Tick), "Delivered transits should leave the queue.");
    AssertTrue(delivery.Delivery.Message.Contains("days in transit", StringComparison.Ordinal), "Delivery ledger should explain transit delay.");
}

static void PrototypeRouteThroughputMetricsAreDeterministic()
{
    var bridge = new SimulationBridge();
    var first = bridge.CreatePrototypeSession(20260429);
    var second = bridge.CreatePrototypeSession(20260429);
    var contract = FindContract(first.Current, "Expected seed 20260429 to expose a dispatchable wood route operation.", candidate => candidate.ResourceId == "wood" && candidate.ExpectedNet > 0m);

    AssertEqual(0, first.Current.RouteThroughput.TotalDispatches);
    AssertEqual(0, first.Current.RouteThroughput.TotalArrivals);
    AssertTrue(first.SelectRouteContract(contract.Id), $"Expected first session to activate {contract.Id}.");
    AssertTrue(second.SelectRouteContract(contract.Id), $"Expected second session to activate {contract.Id}.");

    var firstDelivery = AdvanceUntilRouteOperationDelivery(first, contract.RouteId, contract.ResourceId, maxTicks: 8);
    var secondDelivery = AdvanceUntilRouteOperationDelivery(second, contract.RouteId, contract.ResourceId, maxTicks: 8);

    AssertEqual(first.Current.SaveHash, second.Current.SaveHash);
    AssertEqual(firstDelivery.Snapshot.Tick, secondDelivery.Snapshot.Tick);
    AssertEqual(first.Current.RouteThroughput.TotalDispatches, second.Current.RouteThroughput.TotalDispatches);
    AssertEqual(first.Current.RouteThroughput.TotalArrivals, second.Current.RouteThroughput.TotalArrivals);
    AssertEqual(first.Current.RouteThroughput.TotalUnitsDispatched, second.Current.RouteThroughput.TotalUnitsDispatched);
    AssertEqual(first.Current.RouteThroughput.TotalUnitsArrived, second.Current.RouteThroughput.TotalUnitsArrived);
    AssertEqual(first.Current.RouteThroughput.TotalUnmetDemandServed, second.Current.RouteThroughput.TotalUnmetDemandServed);
    AssertTrue(first.Current.RouteThroughput.TotalDispatches > 0, "Expected route operations to record at least one dispatch.");
    AssertTrue(first.Current.RouteThroughput.TotalArrivals > 0, "Expected route operations to record at least one arrival.");
    AssertTrue(first.Current.RouteThroughput.TotalUnitsDispatched >= first.Current.RouteThroughput.TotalUnitsArrived, "Arrived units should not exceed dispatched route-operation units.");
    AssertTrue(first.Current.RouteThroughput.TotalUnmetDemandServed <= first.Current.RouteThroughput.TotalUnitsArrived, "Unmet demand served should not exceed arrived units.");
}

static void PrototypeRouteOperationStopPreventsSelectedObjectiveCredit()
{
    var session = new SimulationBridge().CreatePrototypeSession(424242);
    var contracts = session.Current.AvailableContracts
        .Where(contract => contract.ExpectedNet > 0m)
        .GroupBy(contract => contract.RouteId, StringComparer.Ordinal)
        .Select(group => group.OrderByDescending(contract => contract.ShipmentPriority).ThenByDescending(contract => contract.ExpectedNet).First())
        .Take(2)
        .ToArray();

    AssertTrue(contracts.Length >= 2, "Expected two route operations for selected-credit regression coverage.");
    AssertTrue(session.SelectRouteContract(contracts[0].Id), "Expected first operation activation.");
    AssertTrue(session.SelectRouteContract(contracts[1].Id), "Expected second operation activation.");
    AssertTrue(session.ClearRouteOperation(contracts[1].RouteId), "Expected stopping the selected route operation to succeed.");

    var completedBefore = session.Current.ScenarioObjective.CompletedCharters;
    for (var i = 0; i < 6; i++)
    {
        session.AdvanceTick();
    }

    AssertTrue(session.Current.ActiveRouteOperations.Count > 0, "Expected another active operation to remain after route-scoped stop.");
    AssertEqual(completedBefore, session.Current.ScenarioObjective.CompletedCharters);
}

static void PrototypeRouteOperationPausesWhenCargoIsBlocked()
{
    var session = new SimulationBridge().CreatePrototypeSession(424242);
    var operation = session.Current.RouteOperationCandidates.First();
    var contract = session.Current.AvailableContracts.Single(candidate => candidate.Id == operation.SourceContractId);

    AssertTrue(session.SelectRouteContract(contract.Id), "Expected route contract selection to start an operation.");
    AssertTrue(session.SetRouteResourceReservation(contract.RouteId, contract.ResourceId, false), "Expected route policy to block the operation cargo.");

    var blocked = session.Current.ActiveRouteOperation;
    AssertTrue(blocked is not null, "Blocked cargo should keep the recurring operation visible.");
    AssertEqual(contract.Id, session.Current.SelectedContractId);
    AssertEqual("paused", blocked!.Status);
    AssertEqual("blocked cargo", blocked.PausedReason);
    AssertTrue(!blocked.CanDispatch, "Blocked operation should not dispatch.");
    AssertTrue(session.Current.AvailableContracts.All(candidate => candidate.Id != contract.Id), "Blocked route cargo should be removed from available contracts.");
    AssertTrue(
        session.Current.NpcPressures.All(pressure => !string.Equals(pressure.RouteOperationId, blocked.Id, StringComparison.Ordinal)),
        "Blocked route operation should not create positive NPC pressure.");

    var tick = session.AdvanceTick();
    AssertTrue(tick.ActiveRouteOperation is not null, "Paused operation should remain visible after a tick.");
    AssertTrue(tick.Ledger.Any(entry => entry.Tick == tick.Tick
        && entry.Category == "Logistics"
        && entry.RelatedId == contract.RouteId
        && entry.Message.Contains("route operation paused", StringComparison.Ordinal)
        && entry.Message.Contains("blocked cargo", StringComparison.Ordinal)), "Paused operation should explain blocked cargo in the ledger.");
}

static void PrototypeScenarioObjectiveIsDeterministic()
{
    var bridge = new SimulationBridge();
    var first = bridge.CreatePrototypeSession(20260429);
    var second = bridge.CreatePrototypeSession(20260429);
    var contractId = first.Current.AvailableContracts.First().Id;

    AssertTrue(first.SelectRouteContract(contractId), "Expected first session route operation selection to succeed.");
    AssertTrue(second.SelectRouteContract(contractId), "Expected second session route operation selection to succeed.");

    for (var i = 0; i < FirstCharterSeason.TickLimit; i++)
    {
        first.AdvanceTick();
        second.AdvanceTick();
    }

    AssertEqual(first.Current.SaveHash, second.Current.SaveHash);
    AssertEqual(ScenarioObjectiveFingerprint(first.Current.ScenarioObjective), ScenarioObjectiveFingerprint(second.Current.ScenarioObjective));
    AssertTrue(first.Current.ScenarioObjective.IsComplete, "Scenario should end deterministically at or before the season limit.");
}

static void PrototypeScenarioObjectiveCountsSelectedDeliveries()
{
    var session = new SimulationBridge().CreatePrototypeSession(20260429);
    var contract = FindContract(session.Current, "Expected seed 20260429 to expose a dispatchable wood route operation.", candidate => candidate.ResourceId == "wood" && candidate.ExpectedNet > 0m);
    var selectedHash = session.Current.SaveHash;

    AssertTrue(session.SelectRouteContract(contract.Id), "Expected route contract selection to start an operation.");
    var dispatch = AdvanceUntilRouteOperationDelivery(session, contract.RouteId, contract.ResourceId);

    AssertTrue(dispatch.Snapshot.SaveHash != selectedHash, "Scenario delivery progress should affect the state hash.");
    AssertEqual(1, dispatch.Snapshot.ScenarioObjective.CompletedCharters);
    AssertEqual(1, dispatch.Snapshot.ScenarioObjective.DistinctResources);
    AssertTrue(dispatch.Snapshot.ScenarioObjective.FinalScore > 0, "Scenario objective should expose a live score after progress.");

    AssertTrue(session.SetRouteResourceReservation(contract.RouteId, contract.ResourceId, false), "Expected route policy to block the active operation after the first delivery.");
    var beforePausedTick = session.Current.ScenarioObjective.CompletedCharters;
    var paused = session.AdvanceTick();
    AssertEqual(beforePausedTick, paused.ScenarioObjective.CompletedCharters);
}

static void FirstCharterSeasonRulesCanBeWon()
{
    AssertEqual(
        FirstCharterSeason.Won,
        FirstCharterSeason.ResolveEndReason(
            FirstCharterSeason.CashTarget,
            FirstCharterSeason.TickLimit,
            FirstCharterSeason.RequiredCharterDeliveries,
            FirstCharterSeason.RequiredDistinctResources,
            FirstCharterSeason.RequiredStableNeeds));
    AssertEqual(
        FirstCharterSeason.Bankrupt,
        FirstCharterSeason.ResolveEndReason(
            -0.01m,
            FirstCharterSeason.TickLimit,
            FirstCharterSeason.RequiredCharterDeliveries,
            FirstCharterSeason.RequiredDistinctResources,
            FirstCharterSeason.RequiredStableNeeds));
    AssertEqual(
        FirstCharterSeason.Timeout,
        FirstCharterSeason.ResolveEndReason(
            FirstCharterSeason.CashTarget,
            FirstCharterSeason.TickLimit,
            FirstCharterSeason.RequiredCharterDeliveries - 1,
            FirstCharterSeason.RequiredDistinctResources,
            FirstCharterSeason.RequiredStableNeeds));
}

static void ScriptedFirstCharterSeasonCanWinABenchmarkSeed()
{
    var bridge = new SimulationBridge();
    var outcomes = Enumerable.Range(1000, 25)
        .Select(seed =>
        {
            var session = bridge.CreatePrototypeSession(seed);
            var result = FirstCharterSeasonScriptedStrategy.Run(session);
            return new ScriptedSeasonOutcome(seed, result, session.Current.SaveHash);
        })
        .ToArray();
    var winner = outcomes.FirstOrDefault(outcome => string.Equals(outcome.Result.EndReason, FirstCharterSeason.Won, StringComparison.Ordinal));

    AssertTrue(
        winner is not null,
        "Expected at least one scripted benchmark seed to win First Charter Season. Outcomes: "
            + string.Join("; ", outcomes.Select(outcome =>
                $"{outcome.Seed}:{outcome.Result.EndReason}:{outcome.Result.CompletedCharters}/{outcome.Result.DistinctResources}/{outcome.Result.StableNeeds}:{outcome.Result.ScenarioScore}")));

    AssertTrue(winner!.Result.WinTick is not null, "Winning scripted run should report a win tick.");
    AssertTrue(winner.Result.WinTick <= FirstCharterSeason.TickLimit, "Scripted win tick should stay inside the season limit.");
    AssertTrue(winner.Result.CompletedCharters >= FirstCharterSeason.RequiredCharterDeliveries, "Scripted win should satisfy charter deliveries.");
    AssertTrue(winner.Result.DistinctResources >= FirstCharterSeason.RequiredDistinctResources, "Scripted win should satisfy resource variety.");
    AssertTrue(winner.Result.StableNeeds >= FirstCharterSeason.RequiredStableNeeds, "Scripted win should satisfy stable needs.");
    AssertTrue(winner.Result.FinalCash >= FirstCharterSeason.CashTarget, "Scripted win should satisfy the cash target.");
    AssertTrue(winner.Result.ProductionFocusChanges > 0, "Scripted strategy should use production focus.");
    AssertTrue(winner.Result.RouteSelections > 0, "Scripted strategy should select route operations.");

    var replay = bridge.CreatePrototypeSession(winner.Seed);
    var replayResult = FirstCharterSeasonScriptedStrategy.Run(replay);
    AssertEqual(winner.Result, replayResult);
    AssertEqual(winner.FinalHash, replay.Current.SaveHash);
}

static void PrototypeScenarioObjectiveStabilityIgnoresLoweredPolicy()
{
    var control = new SimulationBridge().CreatePrototypeSession(424242);
    var lowered = new SimulationBridge().CreatePrototypeSession(424242);
    var target = lowered.Current.Cities
        .SelectMany(city => city.MarketSignals.Select(signal => new { City = city, Signal = signal }))
        .Where(item => item.Signal.DesiredStock > 0 && item.Signal.MarketStock < item.Signal.ReorderPoint)
        .OrderBy(item => item.City.Id, StringComparer.Ordinal)
        .ThenBy(item => item.Signal.ResourceId, StringComparer.Ordinal)
        .FirstOrDefault();

    AssertTrue(target is not null, "Expected at least one need below the default scenario stability threshold.");
    AssertTrue(lowered.SetWarehousePolicy(target!.City.Id, target.Signal.ResourceId, 0, 0), "Expected low warehouse policy override to apply.");

    for (var i = 0; i < FirstCharterSeason.StabilityWindowTicks; i++)
    {
        control.AdvanceTick();
        lowered.AdvanceTick();
    }

    AssertEqual(control.Current.ScenarioObjective.StableNeeds, lowered.Current.ScenarioObjective.StableNeeds);
}

static void PrototypeScenarioObjectiveTimesOutWithoutCharters()
{
    var session = new SimulationBridge().CreatePrototypeSession(424242);

    for (var i = 0; i < FirstCharterSeason.TickLimit; i++)
    {
        session.AdvanceTick();
    }

    var objective = session.Current.ScenarioObjective;
    AssertEqual(FirstCharterSeason.Timeout, objective.EndReason);
    AssertTrue(objective.IsComplete, "Scenario should be complete after the season limit.");
    AssertEqual(0, objective.CompletedCharters);
    AssertTrue(session.Current.Ledger.Any(entry =>
        entry.Category == "Scenario"
        && entry.Message.Contains(FirstCharterSeason.Label, StringComparison.Ordinal)
        && entry.Message.Contains("timeout", StringComparison.Ordinal)), "Scenario timeout should be recorded in the ledger.");
}

static void PrototypeNpcPressureIsDeterministic()
{
    var bridge = new SimulationBridge();
    var first = bridge.CreatePrototypeSession(424242);
    var second = bridge.CreatePrototypeSession(424242);

    AssertTrue(first.Current.NpcPressures.Count > 0, "Expected starter session to expose NPC pressure candidates.");
    AssertEqual(
        NpcPressureFingerprint(first.Current.NpcPressures),
        NpcPressureFingerprint(second.Current.NpcPressures));
    AssertTrue(IsReadOnlySnapshotObjectList(first.Current.NpcPressures), "NPC pressure should be exposed as a read-only snapshot list.");

    first.AdvanceTick();
    second.AdvanceTick();

    AssertEqual(first.Current.SaveHash, second.Current.SaveHash);
    AssertEqual(
        NpcPressureFingerprint(first.Current.NpcPressures),
        NpcPressureFingerprint(second.Current.NpcPressures));
    AssertTrue(first.Current.Ledger.Any(entry =>
        entry.Category == "AI"
        && entry.Message.Contains("pressure", StringComparison.Ordinal)
        && entry.Message.Contains(first.Current.NpcPressures.First().CompanyName, StringComparison.Ordinal)), "AI ledger should explain the top NPC pressure.");
}

static void PrototypeNpcPressureOrderingIsStable()
{
    var snapshot = new SimulationBridge().CreatePrototypeSession(20260429).Current;
    AssertTrue(snapshot.NpcPressures.Any(pressure => pressure.RouteOperationId is not null), "Expected NPC pressure to consume route operation candidates.");
    AssertTrue(snapshot.NpcPressures.Any(pressure => pressure.ProductionOpportunityId is not null), "Expected NPC pressure to consume production opportunities.");
    foreach (var pressure in snapshot.NpcPressures.Where(pressure => pressure.ProductionOpportunityId is not null))
    {
        var chain = snapshot.ProductionChainOpportunities.Single(opportunity => opportunity.Id == pressure.ProductionOpportunityId);
        AssertEqual(chain.CityId, pressure.CityId);
    }

    for (var i = 1; i < snapshot.NpcPressures.Count; i++)
    {
        var previous = snapshot.NpcPressures[i - 1];
        var current = snapshot.NpcPressures[i];
        AssertTrue(previous.Pressure >= current.Pressure, "NPC pressure should sort by pressure first.");
        if (previous.Pressure == current.Pressure)
        {
            AssertTrue(previous.ShipmentPriority >= current.ShipmentPriority, "NPC pressure should break pressure ties by shipment priority.");
        }

        if (previous.Pressure == current.Pressure && previous.ShipmentPriority == current.ShipmentPriority)
        {
            AssertTrue(previous.ExpectedValue >= current.ExpectedValue, "NPC pressure should break priority ties by expected value.");
        }

        if (previous.Pressure == current.Pressure
            && previous.ShipmentPriority == current.ShipmentPriority
            && previous.ExpectedValue == current.ExpectedValue)
        {
            var previousTieBreak = $"{previous.CompanyId}:{previous.Intent}:{previous.Id}";
            var currentTieBreak = $"{current.CompanyId}:{current.Intent}:{current.Id}";
            AssertTrue(string.CompareOrdinal(previousTieBreak, currentTieBreak) <= 0, "NPC pressure should use ordinal ids after company and intent tie-breaks.");
        }
    }
}

static void NpcPressureScorerTieBreaksAndBlocksSafely()
{
    var ai = new DeterministicNpcPressureAi();
    var right = new NpcPressureCandidate(
        "npc:test:route:b",
        "contest_route",
        "node_001",
        "route_001",
        "route_001:node_002->node_001:grain",
        null,
        "grain",
        25m,
        3,
        2,
        true,
        4m,
        "same score b");
    var left = right with { Id = "npc:test:route:a", RouteOperationId = "route_001:node_003->node_001:grain", Reason = "same score a" };

    var ranked = ai.Rank("test_company", [right, left]).ToArray();
    AssertEqual("npc:test:route:a", ranked[0].CandidateId);
    AssertEqual("npc:test:route:b", ranked[1].CandidateId);

    var blocked = right with
    {
        Id = "npc:test:route:blocked",
        CanContest = false,
        ExpectedValue = 200m,
        ShipmentPriority = 4,
        DemandServed = 12,
        StrategicBonus = 20m,
        Reason = "blocked cargo"
    };
    AssertEqual(0m, ai.Score("test_company", blocked).Pressure);
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
            new WarehousePolicySaveState(snapshot.World.Nodes[0].Id, "grain", 5, 12, PrototypeSession.ConservativeWarehouseMode)
        ]
    };
    var firstHash = SaveCodec.ComputeStateHash(save);
    var json = SaveCodec.Serialize(save);
    var loaded = SaveCodec.Deserialize(json);
    var secondHash = SaveCodec.ComputeStateHash(loaded);

    AssertEqual(firstHash, secondHash);
    AssertTrue(firstHash != SaveCodec.ComputeStateHash(save with { WarehousePolicies = [] }), "Warehouse policy saves should affect state hash.");
    AssertTrue(firstHash != SaveCodec.ComputeStateHash(save with { WarehousePolicies = [save.WarehousePolicies[0] with { Mode = null }] }), "Non-default warehouse policy mode should affect state hash.");
    AssertEqual(
        SaveCodec.ComputeStateHash(save with { WarehousePolicies = [save.WarehousePolicies[0] with { Mode = null }] }),
        SaveCodec.ComputeStateHash(save with { WarehousePolicies = [save.WarehousePolicies[0] with { Mode = PrototypeSession.BalancedWarehouseMode }] }));

    try
    {
        SaveCodec.ComputeStateHash(save with { SaveVersion = 1 });
        throw new InvalidOperationException("Expected save validation to reject an old save version without migration.");
    }
    catch (SaveValidationException ex)
    {
        AssertTrue(ex.Errors.Any(error => error.Contains("saveVersion", StringComparison.Ordinal)), "Expected save version validation error.");
    }

    try
    {
        SaveCodec.Serialize(save with { WarehousePolicies = [save.WarehousePolicies[0] with { Mode = "reckless" }] });
        throw new InvalidOperationException("Expected save validation to reject unknown warehouse policy modes.");
    }
    catch (SaveValidationException ex)
    {
        AssertTrue(ex.Errors.Any(error => error.Contains("mode must be balanced or conservative", StringComparison.Ordinal)), "Expected warehouse policy mode validation error.");
    }
}

static void ProductionPolicySaveLoadPreservesHash()
{
    var content = GameContentLoader.LoadFromDirectory(ContentPathResolver.FindContentDirectory());
    var snapshot = new SimulationBridge().CreateNewGame(424242);
    var routes = RoutePlanner.FromWorld(snapshot.World);
    var market = StarterScenarioFactory.CreateInitialMarket(content.Resources);
    var prices = new EconomyTick().CalculatePrices(content.Resources, market, StarterScenarioFactory.CreateNeeds(content.Resources));
    var cityId = snapshot.World.Nodes[0].Id;
    var save = StarterSaveFactory.Create(424242, snapshot.World.WorldGenVersion, content.ContentHash, snapshot.World.Nodes, routes, content.Resources, market, prices) with
    {
        ProductionPolicies =
        [
            new ProductionPolicySaveState(cityId, "grain_fields", PrototypeSession.FocusProductionMode)
        ]
    };

    var firstHash = SaveCodec.ComputeStateHash(save);
    var loaded = SaveCodec.Deserialize(SaveCodec.Serialize(save));
    var secondHash = SaveCodec.ComputeStateHash(loaded);

    AssertEqual(firstHash, secondHash);
    AssertTrue(firstHash != SaveCodec.ComputeStateHash(save with { ProductionPolicies = [] }), "Production policy saves should affect state hash.");
    AssertEqual(1, loaded.ProductionPolicies.Count);
    AssertEqual(PrototypeSession.FocusProductionMode, loaded.ProductionPolicies[0].Mode);
    AssertEqual(
        SaveCodec.ComputeStateHash(save with { ProductionPolicies = [] }),
        SaveCodec.ComputeStateHash(save with { ProductionPolicies = [new ProductionPolicySaveState(cityId, null, PrototypeSession.AutoProductionMode)] }));
}

static void ProductionPolicyValidationRejectsInvalidState()
{
    var content = GameContentLoader.LoadFromDirectory(ContentPathResolver.FindContentDirectory());
    var snapshot = new SimulationBridge().CreateNewGame(424242);
    var routes = RoutePlanner.FromWorld(snapshot.World);
    var market = StarterScenarioFactory.CreateInitialMarket(content.Resources);
    var prices = new EconomyTick().CalculatePrices(content.Resources, market, StarterScenarioFactory.CreateNeeds(content.Resources));
    var save = StarterSaveFactory.Create(424242, snapshot.World.WorldGenVersion, content.ContentHash, snapshot.World.Nodes, routes, content.Resources, market, prices);
    var cityId = snapshot.World.Nodes[0].Id;

    try
    {
        SaveCodec.Serialize(save with { ProductionPolicies = [new ProductionPolicySaveState(cityId, null, PrototypeSession.FocusProductionMode)] });
        throw new InvalidOperationException("Expected save validation to reject focus mode without a recipe.");
    }
    catch (SaveValidationException ex)
    {
        AssertTrue(ex.Errors.Any(error => error.Contains("focusRecipeId must be present", StringComparison.Ordinal)), "Expected focusRecipeId validation error.");
    }

    try
    {
        SaveCodec.Serialize(save with { ProductionPolicies = [new ProductionPolicySaveState(cityId, "grain_fields", PrototypeSession.AutoProductionMode)] });
        throw new InvalidOperationException("Expected save validation to reject auto mode with a recipe.");
    }
    catch (SaveValidationException ex)
    {
        AssertTrue(ex.Errors.Any(error => error.Contains("must only be present in focus mode", StringComparison.Ordinal)), "Expected auto recipe validation error.");
    }

    try
    {
        SaveCodec.Serialize(save with { ProductionPolicies = [new ProductionPolicySaveState("missing-city", null, PrototypeSession.PausedProductionMode)] });
        throw new InvalidOperationException("Expected save validation to reject an unknown production policy city.");
    }
    catch (SaveValidationException ex)
    {
        AssertTrue(ex.Errors.Any(error => error.Contains("must reference a saved city", StringComparison.Ordinal)), "Expected production policy city validation error.");
    }

    try
    {
        SaveCodec.Serialize(save with { ProductionPolicies = [new ProductionPolicySaveState(cityId, null, "rush")] });
        throw new InvalidOperationException("Expected save validation to reject unknown production policy modes.");
    }
    catch (SaveValidationException ex)
    {
        AssertTrue(ex.Errors.Any(error => error.Contains("mode must be auto, focus, or paused", StringComparison.Ordinal)), "Expected production policy mode validation error.");
    }
}

static void ScenarioObjectiveSaveLoadPreservesHash()
{
    var content = GameContentLoader.LoadFromDirectory(ContentPathResolver.FindContentDirectory());
    var snapshot = new SimulationBridge().CreateNewGame(424242);
    var routes = RoutePlanner.FromWorld(snapshot.World);
    var market = StarterScenarioFactory.CreateInitialMarket(content.Resources);
    var prices = new EconomyTick().CalculatePrices(content.Resources, market, StarterScenarioFactory.CreateNeeds(content.Resources));
    var save = StarterSaveFactory.Create(424242, snapshot.World.WorldGenVersion, content.ContentHash, snapshot.World.Nodes, routes, content.Resources, market, prices);
    var progressed = save with
    {
        ScenarioObjective = new ScenarioObjectiveSaveState(
            FirstCharterSeason.ScenarioId,
            FirstCharterSeason.RulesVersion,
            StartedTick: 0,
            CurrentTick: 7,
            EndTick: null,
            FirstCharterSeason.InProgress,
            ["7:route_001:node_001->node_002:grain"],
            ["grain"],
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["node_001:grain"] = FirstCharterSeason.StabilityWindowTicks
            },
            FinalCash: 1135m,
            FinalScore: 42)
    };

    var firstHash = SaveCodec.ComputeStateHash(progressed);
    var loaded = SaveCodec.Deserialize(SaveCodec.Serialize(progressed));
    var secondHash = SaveCodec.ComputeStateHash(loaded);

    AssertEqual(firstHash, secondHash);
    AssertTrue(firstHash != SaveCodec.ComputeStateHash(save), "Scenario objective progress should affect state hash.");
    AssertEqual(FirstCharterSeason.InProgress, loaded.ScenarioObjective.EndReason);
    AssertEqual(1, loaded.ScenarioObjective.CompletedCharterIds.Count);
}

static void RouteOperationSaveLoadPreservesHash()
{
    var content = GameContentLoader.LoadFromDirectory(ContentPathResolver.FindContentDirectory());
    var snapshot = new SimulationBridge().CreateNewGame(424242);
    var routes = RoutePlanner.FromWorld(snapshot.World);
    var market = StarterScenarioFactory.CreateInitialMarket(content.Resources);
    var prices = new EconomyTick().CalculatePrices(content.Resources, market, StarterScenarioFactory.CreateNeeds(content.Resources));
    var route = routes[0];
    var operationId = $"{route.Id}:{route.FromNode}->{route.ToNode}:grain";
    var save = StarterSaveFactory.Create(424242, snapshot.World.WorldGenVersion, content.ContentHash, snapshot.World.Nodes, routes, content.Resources, market, prices) with
    {
        RouteOperations =
        [
            new RouteOperationSaveState(operationId, $"{route.Id}:grain", route.Id, route.FromNode, route.ToNode, "grain", UnitsPerDispatch: 2)
        ],
        RouteTransits =
        [
            new RouteTransitSaveState($"{operationId}:dispatch-0001-00", operationId, route.Id, route.FromNode, route.ToNode, "grain", Units: 2, DispatchedTick: 1, ArrivalTick: 1 + route.LeadDays, ExpectedRevenue: 20m, TransportCost: 3m)
        ],
        PendingRouteContractId = $"{route.Id}:grain"
    };

    var firstHash = SaveCodec.ComputeStateHash(save);
    var loaded = SaveCodec.Deserialize(SaveCodec.Serialize(save));
    var secondHash = SaveCodec.ComputeStateHash(loaded);

    AssertEqual(firstHash, secondHash);
    AssertTrue(firstHash != SaveCodec.ComputeStateHash(save with { RouteOperations = [], RouteTransits = [], PendingRouteContractId = null }), "Route operations and transit queue should affect state hash.");
    AssertEqual(1, loaded.RouteOperations.Count);
    AssertEqual(1, loaded.RouteTransits.Count);
}

static void RouteTransitValidationRejectsInvalidState()
{
    var content = GameContentLoader.LoadFromDirectory(ContentPathResolver.FindContentDirectory());
    var snapshot = new SimulationBridge().CreateNewGame(424242);
    var routes = RoutePlanner.FromWorld(snapshot.World);
    var market = StarterScenarioFactory.CreateInitialMarket(content.Resources);
    var prices = new EconomyTick().CalculatePrices(content.Resources, market, StarterScenarioFactory.CreateNeeds(content.Resources));
    var route = routes[0];
    var operationId = $"{route.Id}:{route.FromNode}->{route.ToNode}:grain";
    var save = StarterSaveFactory.Create(424242, snapshot.World.WorldGenVersion, content.ContentHash, snapshot.World.Nodes, routes, content.Resources, market, prices) with
    {
        RouteOperations =
        [
            new RouteOperationSaveState(operationId, $"{route.Id}:grain", route.Id, route.FromNode, route.ToNode, "grain", UnitsPerDispatch: 2)
        ],
        RouteTransits =
        [
            new RouteTransitSaveState($"{operationId}:dispatch-0001-00", operationId, route.Id, route.FromNode, route.ToNode, "grain", Units: 0, DispatchedTick: 2, ArrivalTick: 2, ExpectedRevenue: -1m, TransportCost: -1m)
        ]
    };

    try
    {
        SaveCodec.Serialize(save);
        throw new InvalidOperationException("Expected save validation to reject invalid transit state.");
    }
    catch (SaveValidationException ex)
    {
        AssertTrue(ex.Errors.Any(error => error.Contains("units must be positive", StringComparison.Ordinal)), "Expected transit units validation error.");
        AssertTrue(ex.Errors.Any(error => error.Contains("arrivalTick must be after dispatchedTick", StringComparison.Ordinal)), "Expected transit timing validation error.");
        AssertTrue(ex.Errors.Any(error => error.Contains("expectedRevenue must not be negative", StringComparison.Ordinal)), "Expected transit revenue validation error.");
        AssertTrue(ex.Errors.Any(error => error.Contains("transportCost must not be negative", StringComparison.Ordinal)), "Expected transit cost validation error.");
    }

    var orphanTransit = save with
    {
        RouteTransits =
        [
            new RouteTransitSaveState($"{operationId}:dispatch-0002-00", "missing-operation", route.Id, route.FromNode, route.ToNode, "grain", Units: 1, DispatchedTick: 2, ArrivalTick: 3, ExpectedRevenue: 1m, TransportCost: 0m)
        ]
    };
    try
    {
        SaveCodec.Serialize(orphanTransit);
        throw new InvalidOperationException("Expected save validation to reject orphan route transit.");
    }
    catch (SaveValidationException ex)
    {
        AssertTrue(ex.Errors.Any(error => error.Contains("must reference a saved route operation", StringComparison.Ordinal)), "Expected transit operation reference validation error.");
    }

    var mismatchedTransit = save with
    {
        RouteTransits =
        [
            new RouteTransitSaveState($"{operationId}:dispatch-0003-00", operationId, route.Id, route.ToNode, route.FromNode, "grain", Units: 1, DispatchedTick: 2, ArrivalTick: 3, ExpectedRevenue: 1m, TransportCost: 0m)
        ]
    };
    try
    {
        SaveCodec.Serialize(mismatchedTransit);
        throw new InvalidOperationException("Expected save validation to reject transit that mismatches its route operation.");
    }
    catch (SaveValidationException ex)
    {
        AssertTrue(ex.Errors.Any(error => error.Contains("must match its saved route operation", StringComparison.Ordinal)), "Expected transit operation mismatch validation error.");
    }
}

static void ScenarioObjectiveValidationRejectsInvalidState()
{
    var content = GameContentLoader.LoadFromDirectory(ContentPathResolver.FindContentDirectory());
    var snapshot = new SimulationBridge().CreateNewGame(424242);
    var routes = RoutePlanner.FromWorld(snapshot.World);
    var market = StarterScenarioFactory.CreateInitialMarket(content.Resources);
    var prices = new EconomyTick().CalculatePrices(content.Resources, market, StarterScenarioFactory.CreateNeeds(content.Resources));
    var save = StarterSaveFactory.Create(424242, snapshot.World.WorldGenVersion, content.ContentHash, snapshot.World.Nodes, routes, content.Resources, market, prices);
    var invalid = save with
    {
        ScenarioObjective = save.ScenarioObjective with
        {
            EndReason = FirstCharterSeason.Won,
            EndTick = null
        }
    };

    try
    {
        SaveCodec.Serialize(invalid);
        throw new InvalidOperationException("Expected save validation to reject an ended scenario without endTick.");
    }
    catch (SaveValidationException ex)
    {
        AssertTrue(ex.Errors.Any(error => error.Contains("endTick must be present", StringComparison.Ordinal)), "Expected scenario endTick validation error.");
    }

    var whitespaceInvalid = save with
    {
        ScenarioObjective = save.ScenarioObjective with
        {
            CompletedCharterIds = [" charter_001"],
            StableNeedStreaks = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["node_001:grain"] = 1,
                ["node_001:grain "] = 1
            }
        }
    };

    try
    {
        SaveCodec.Serialize(whitespaceInvalid);
        throw new InvalidOperationException("Expected save validation to reject scenario ids with surrounding whitespace.");
    }
    catch (SaveValidationException ex)
    {
        AssertTrue(ex.Errors.Any(error => error.Contains("surrounding whitespace", StringComparison.Ordinal)), "Expected scenario whitespace validation error.");
        AssertTrue(ex.Errors.Any(error => error.Contains("must not be duplicated", StringComparison.Ordinal)), "Expected trimmed stable need duplicate validation error.");
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

static void SimulationCoreProjectsRemainGodotFree()
{
    var root = FindRepositoryRoot();
    var coreProjects = new[]
    {
        "AI.Company",
        "CitySim.Core",
        "Content.Core",
        "Economy.Core",
        "GodotBridge",
        "Logistics.Core",
        "Persistence.Core",
        "WorldGen.Core"
    };

    foreach (var projectName in coreProjects)
    {
        var projectDir = Path.Combine(root, "src", projectName);
        var projectFile = Directory.EnumerateFiles(projectDir, "*.csproj").Single();
        var projectText = File.ReadAllText(projectFile);
        AssertTrue(!projectText.Contains("Godot.NET.Sdk", StringComparison.Ordinal), $"{projectName} must not use the Godot SDK.");
        AssertTrue(!projectText.Contains("PackageReference Include=\"Godot", StringComparison.Ordinal), $"{projectName} must not package-reference Godot.");

        foreach (var sourceFile in Directory.EnumerateFiles(projectDir, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(sourceFile);
            AssertTrue(!text.Contains("using Godot;", StringComparison.Ordinal), $"{projectName} must not import Godot in {Path.GetFileName(sourceFile)}.");
            AssertTrue(!text.Contains("Godot.", StringComparison.Ordinal), $"{projectName} must not call Godot APIs in {Path.GetFileName(sourceFile)}.");
        }
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

static string RouteOperationFingerprint(IEnumerable<PrototypeRouteOperationView> operations)
{
    return string.Join("|", operations.Select(operation =>
        string.Format(
            CultureInfo.InvariantCulture,
            "{0}:{1}:{2}->{3}:{4}:{5}:{6}:{7}:{8}:{9}:{10}:{11:0.00}:{12:0.00}:{13:0.00}:{14}:{15}:{16}:{17}",
            operation.Id,
            operation.SourceContractId,
            operation.FromNode,
            operation.ToNode,
            operation.RouteId,
            operation.ResourceId,
            operation.IsActive,
            operation.CanDispatch,
            operation.CapacityPerDay,
            operation.ExpectedUnits,
            operation.ShipmentPriority,
            operation.ExpectedRevenue,
            operation.TransportCost,
            operation.ExpectedNet,
            operation.UnmetDemandServed,
            operation.Status,
            operation.PausedReason,
            operation.PolicyAction)));
}

static string ScenarioObjectiveFingerprint(PrototypeScenarioObjectiveView objective)
{
    return string.Format(
        CultureInfo.InvariantCulture,
        "{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9:0.00}:{10}",
        objective.ScenarioId,
        objective.RulesVersion,
        objective.CurrentTick,
        objective.EndReason,
        objective.CompletedCharters,
        objective.RequiredCharters,
        objective.DistinctResources,
        objective.StableNeeds,
        objective.FinalScore,
        objective.CurrentCash,
        objective.NextStep);
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

static (PrototypeSnapshot Snapshot, PrototypeLedgerEntry Delivery) AdvanceUntilRouteOperationDelivery(
    PrototypeSession session,
    string routeId,
    string resourceId,
    int maxTicks = 6)
{
    for (var i = 0; i < maxTicks; i++)
    {
        var previousCash = session.Current.Company.Cash;
        var tick = session.AdvanceTick();
        var tickLedger = tick.Ledger.Where(entry => entry.Tick == tick.Tick).ToArray();
        AssertEqual(tick.Company.Cash - previousCash, tickLedger.Sum(entry => entry.CashDelta));

        var delivery = tickLedger.FirstOrDefault(entry =>
            entry.Category == "Logistics"
            && entry.RelatedId == routeId
            && entry.Message.Contains("route operation delivered", StringComparison.Ordinal)
            && entry.Message.Contains(resourceId, StringComparison.Ordinal));
        if (delivery is not null)
        {
            return (tick, delivery);
        }
    }

    throw new InvalidOperationException($"Expected selected route operation {routeId}:{resourceId} to dispatch within {maxTicks} ticks.");
}

static string CitySpecializationFingerprint(PrototypeSnapshot snapshot)
{
    return string.Join("|", snapshot.Cities
        .OrderBy(city => city.Id, StringComparer.Ordinal)
        .Select(city =>
            string.Format(
                CultureInfo.InvariantCulture,
                "{0}:{1}:{2}:{3}:{4}:{5}",
                city.Id,
                string.Join(",", city.Districts.Order(StringComparer.Ordinal)),
                city.Specialization.RoleId,
                city.Specialization.Label,
                string.Join(",", city.Specialization.AnchorResources.Order(StringComparer.Ordinal)),
                string.Join(",", city.Specialization.OutputResources.Order(StringComparer.Ordinal)))));
}

static string ProductionChainFingerprint(IEnumerable<PrototypeProductionChainOpportunityView> opportunities)
{
    return string.Join("|", opportunities.Select(opportunity =>
        string.Format(
            CultureInfo.InvariantCulture,
            "{0}:{1}:{2}:{3}:{4}:{5:0.00}:{6:0.00}:{7:0.00}:{8}:{9}:{10}:{11}:{12}:{13}",
            opportunity.Id,
            opportunity.CityId,
            opportunity.RecipeId,
            opportunity.IsReady ? "ready" : "blocked",
            opportunity.MaxRunsFromWarehouse,
            opportunity.InputCost,
            opportunity.OutputValue,
            opportunity.ExpectedMargin,
            opportunity.BottleneckResourceId ?? "",
            opportunity.MissingInputUnits,
            opportunity.DestinationShipmentPriority,
            opportunity.CandidateRouteId ?? "",
            ProductionLineFingerprint(opportunity.Inputs),
            ProductionLineFingerprint(opportunity.Outputs))));
}

static string ProductionLineFingerprint(IEnumerable<PrototypeProductionResourceLineView> lines)
{
    return string.Join(",", lines
        .OrderBy(line => line.ResourceId, StringComparer.Ordinal)
        .Select(line =>
            string.Format(
                CultureInfo.InvariantCulture,
                "{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8:0.00}:{9:0.0000}:{10}:{11}:{12:0.00}:{13}",
                line.ResourceId,
                line.RequiredAmount,
                line.OutputAmount,
                line.WarehouseStock,
                line.MarketStock,
                line.ProtectedStock,
                line.AvailableAmount,
                line.MissingAmount,
                line.LocalUnitPrice,
                line.LocalScarcity,
                line.BestDestinationCityId ?? "",
                line.BestRouteId ?? "",
                line.BestDestinationUnitPrice ?? 0m,
                line.DestinationShipmentPriority)));
}

static string ProductionPolicyFingerprint(PrototypeSnapshot snapshot)
{
    return string.Join("|", snapshot.ProductionPolicies
        .OrderBy(policy => policy.CityId, StringComparer.Ordinal)
        .Select(policy => string.Format(
            CultureInfo.InvariantCulture,
            "{0}:{1}:{2}:{3}",
            policy.CityId,
            policy.Mode,
            policy.FocusRecipeId ?? "",
            policy.Summary)));
}

static string NpcPressureFingerprint(IEnumerable<PrototypeNpcPressureView> pressures)
{
    return string.Join("|", pressures.Select(pressure =>
        string.Format(
            CultureInfo.InvariantCulture,
            "{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9:0.00}:{10}:{11:0.00}:{12}:{13}:{14}:{15}",
            pressure.Id,
            pressure.CompanyId,
            pressure.Intent,
            pressure.CityId,
            pressure.TargetCityId ?? "",
            pressure.RouteId ?? "",
            pressure.RouteOperationId ?? "",
            pressure.ProductionOpportunityId ?? "",
            pressure.ResourceId,
            pressure.Pressure,
            pressure.ShipmentPriority,
            pressure.ExpectedValue,
            pressure.CanContest,
            pressure.Reason,
            pressure.CompanyName,
            pressure.TargetCityName ?? "")));
}

static string FindRepositoryRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "ChartersOfTrade.sln")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    throw new InvalidOperationException("Could not locate repository root from test output directory.");
}

static int FirstIndex<T>(IReadOnlyList<T> values, Func<T, bool> predicate)
{
    for (var i = 0; i < values.Count; i++)
    {
        if (predicate(values[i]))
        {
            return i;
        }
    }

    return -1;
}

static int LastIndex<T>(IReadOnlyList<T> values, Func<T, bool> predicate)
{
    for (var i = values.Count - 1; i >= 0; i--)
    {
        if (predicate(values[i]))
        {
            return i;
        }
    }

    return -1;
}

static string PolicyFingerprint(PrototypeSnapshot snapshot)
{
    return string.Join("|", snapshot.Cities
        .OrderBy(city => city.Id, StringComparer.Ordinal)
        .SelectMany(city => city.MarketSignals.Select(signal =>
            string.Format(
                CultureInfo.InvariantCulture,
                "{0}:{1}:{2}:{3}:{4}:{5}:{6}",
                city.Id,
                signal.ResourceId,
                signal.SafetyStock,
                signal.ReorderPoint,
                signal.IsPolicyOverridden,
                signal.PolicyMode,
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

internal sealed record ScriptedSeasonOutcome(
    int Seed,
    FirstCharterSeasonScriptedRunResult Result,
    string FinalHash);
