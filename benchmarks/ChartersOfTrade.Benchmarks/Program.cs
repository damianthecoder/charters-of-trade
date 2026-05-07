using System.Globalization;
using ChartersOfTrade.GodotBridge;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var seeds = Enumerable.Range(1000, 25).ToArray();
var bridge = new SimulationBridge();
var rows = new List<BenchmarkRow>();

foreach (var seed in seeds)
{
    var naive = RunNaiveSeason(bridge.CreatePrototypeSession(seed));
    var scriptedSession = bridge.CreatePrototypeSession(seed);
    var scripted = FirstCharterSeasonScriptedStrategy.Run(scriptedSession);
    var scriptedSnapshot = scriptedSession.Current;
    var snapshot = naive.Snapshot;
    var routeProfit = snapshot.Routes.Count == 0 ? 0.0 : snapshot.Routes
        .Select(route => (double)(route.CapacityPerDay * 2 - route.LeadDays))
        .Order()
        .ElementAt(snapshot.Routes.Count / 2);

    rows.Add(new BenchmarkRow(
        seed,
        snapshot.World.Hash,
        snapshot.World.HasSolvencyKernel,
        snapshot.Routes.Count,
        Math.Round(routeProfit, 2),
        Math.Round(snapshot.UnmetDemandRatio, 4),
        naive.TimeToProfit,
        naive.Bankrupt,
        snapshot.Company.Cash,
        snapshot.AiChoice.OpportunityId,
        snapshot.ScenarioObjective.EndReason,
        snapshot.ScenarioObjective.FinalScore,
        snapshot.ScenarioObjective.IsWon ? snapshot.ScenarioObjective.CurrentTick : -1,
        snapshot.ScenarioObjective.CompletedCharters,
        snapshot.ScenarioObjective.DistinctResources,
        snapshot.ScenarioObjective.StableNeeds,
        snapshot.ActiveRouteOperations.Count,
        snapshot.RouteTransits.Count,
        snapshot.RouteThroughput.TotalDispatches,
        snapshot.RouteThroughput.TotalArrivals,
        snapshot.RouteThroughput.TotalUnitsDispatched,
        snapshot.RouteThroughput.TotalUnitsArrived,
        snapshot.RouteThroughput.TotalUnmetDemandServed,
        scripted.EndReason,
        scripted.ScenarioScore,
        scripted.WinTick ?? -1,
        scripted.CompletedCharters,
        scripted.DistinctResources,
        scripted.StableNeeds,
        scripted.FinalCash,
        scriptedSnapshot.RouteThroughput.TotalDispatches,
        scriptedSnapshot.RouteThroughput.TotalArrivals,
        scriptedSnapshot.RouteThroughput.TotalUnitsDispatched,
        scriptedSnapshot.RouteThroughput.TotalUnitsArrived,
        scriptedSnapshot.RouteThroughput.TotalUnmetDemandServed,
        scripted.ProductionFocusChanges,
        scripted.RouteSelections));
}

Console.WriteLine("seed,world_hash,solvency_kernel,route_count,median_route_profit_proxy,unmet_demand_ratio,time_to_profit,bankrupt,cash_after_12,ai_move,scenario_result,scenario_score,scenario_win_tick,total_charter_deliveries,distinct_delivered_resources,stable_need_ticks,active_route_operations,in_transit_shipments,route_dispatches,route_arrivals,units_dispatched,units_arrived,unmet_demand_served,scripted_scenario_result,scripted_scenario_score,scripted_scenario_win_tick,scripted_total_charter_deliveries,scripted_distinct_delivered_resources,scripted_stable_need_ticks,scripted_cash_after_run,scripted_route_dispatches,scripted_route_arrivals,scripted_units_dispatched,scripted_units_arrived,scripted_unmet_demand_served,scripted_focus_changes,scripted_route_selections");
foreach (var row in rows)
{
    Console.WriteLine($"{row.Seed},{row.WorldHash},{row.HasSolvencyKernel},{row.RouteCount},{row.MedianRouteProfitProxy},{row.UnmetDemandRatio},{row.TimeToProfit},{row.Bankrupt},{row.CashAfter12},{row.AiMove},{row.ScenarioResult},{row.ScenarioScore},{row.ScenarioWinTick},{row.TotalCharterDeliveries},{row.DistinctDeliveredResources},{row.StableNeedTicks},{row.ActiveRouteOperations},{row.InTransitShipments},{row.RouteDispatches},{row.RouteArrivals},{row.UnitsDispatched},{row.UnitsArrived},{row.UnmetDemandServed},{row.ScriptedScenarioResult},{row.ScriptedScenarioScore},{row.ScriptedScenarioWinTick},{row.ScriptedTotalCharterDeliveries},{row.ScriptedDistinctDeliveredResources},{row.ScriptedStableNeedTicks},{row.ScriptedCashAfterRun},{row.ScriptedRouteDispatches},{row.ScriptedRouteArrivals},{row.ScriptedUnitsDispatched},{row.ScriptedUnitsArrived},{row.ScriptedUnmetDemandServed},{row.ScriptedFocusChanges},{row.ScriptedRouteSelections}");
}

var playable = rows.Count(row => row.HasSolvencyKernel);
Console.WriteLine();
Console.WriteLine($"Playable seeds: {playable}/{rows.Count}");
Console.WriteLine($"Average unmet demand ratio: {rows.Average(row => row.UnmetDemandRatio):0.0000}");
Console.WriteLine($"Median time to profit: {Median(rows.Where(row => row.TimeToProfit > 0).Select(row => row.TimeToProfit)):0.0}");
Console.WriteLine($"Bankruptcy frequency: {rows.Count(row => row.Bankrupt)}/{rows.Count}");
Console.WriteLine($"Average naive scenario score: {rows.Average(row => row.ScenarioScore):0.0}");
Console.WriteLine($"Naive scenario wins/timeouts/bankruptcies: {rows.Count(row => row.ScenarioResult == FirstCharterSeason.Won)}/{rows.Count(row => row.ScenarioResult == FirstCharterSeason.Timeout)}/{rows.Count(row => row.ScenarioResult == FirstCharterSeason.Bankrupt)}");
Console.WriteLine($"Average active route operations: {rows.Average(row => row.ActiveRouteOperations):0.0}");
Console.WriteLine($"Average in-transit shipments: {rows.Average(row => row.InTransitShipments):0.0}");
Console.WriteLine($"Average route dispatches: {rows.Average(row => row.RouteDispatches):0.0}");
Console.WriteLine($"Average route arrivals: {rows.Average(row => row.RouteArrivals):0.0}");
Console.WriteLine($"Average units dispatched: {rows.Average(row => row.UnitsDispatched):0.0}");
Console.WriteLine($"Average units arrived: {rows.Average(row => row.UnitsArrived):0.0}");
Console.WriteLine($"Average unmet demand served: {rows.Average(row => row.UnmetDemandServed):0.0}");
Console.WriteLine($"Average scripted scenario score: {rows.Average(row => row.ScriptedScenarioScore):0.0}");
Console.WriteLine($"Scripted scenario wins/timeouts/bankruptcies: {rows.Count(row => row.ScriptedScenarioResult == FirstCharterSeason.Won)}/{rows.Count(row => row.ScriptedScenarioResult == FirstCharterSeason.Timeout)}/{rows.Count(row => row.ScriptedScenarioResult == FirstCharterSeason.Bankrupt)}");
Console.WriteLine($"Median scripted win tick: {Median(rows.Where(row => row.ScriptedScenarioWinTick > 0).Select(row => row.ScriptedScenarioWinTick)):0.0}");
Console.WriteLine($"Average scripted charter deliveries: {rows.Average(row => row.ScriptedTotalCharterDeliveries):0.0}");
Console.WriteLine($"Average scripted distinct resources: {rows.Average(row => row.ScriptedDistinctDeliveredResources):0.0}");
Console.WriteLine($"Average scripted stable need ticks: {rows.Average(row => row.ScriptedStableNeedTicks):0.0}");
Console.WriteLine($"Average scripted units arrived: {rows.Average(row => row.ScriptedUnitsArrived):0.0}");
Console.WriteLine($"Average scripted unmet demand served: {rows.Average(row => row.ScriptedUnmetDemandServed):0.0}");
Console.WriteLine($"Average scripted focus changes: {rows.Average(row => row.ScriptedFocusChanges):0.0}");

static NaiveSeasonRun RunNaiveSeason(PrototypeSession session)
{
    foreach (var contract in session.Current.AvailableContracts
        .Where(contract => contract.ExpectedNet > 0m)
        .GroupBy(contract => contract.RouteId, StringComparer.Ordinal)
        .Select(group => group.OrderByDescending(contract => contract.ShipmentPriority).ThenByDescending(contract => contract.ExpectedNet).First())
        .Take(3))
    {
        session.SelectRouteContract(contract.Id);
    }

    var initial = session.Current;
    var timeToProfit = -1;
    var bankrupt = false;

    for (var tick = 1; tick <= FirstCharterSeason.TickLimit; tick++)
    {
        var current = session.AdvanceTick();
        if (timeToProfit < 0 && current.Company.Cash > initial.Company.Cash)
        {
            timeToProfit = tick;
        }

        if (current.Company.Cash < 0)
        {
            bankrupt = true;
        }
    }

    return new NaiveSeasonRun(session.Current, timeToProfit, bankrupt);
}

static double Median(IEnumerable<int> values)
{
    var ordered = values.Order().ToArray();
    if (ordered.Length == 0)
    {
        return -1;
    }

    var middle = ordered.Length / 2;
    return ordered.Length % 2 == 1 ? ordered[middle] : (ordered[middle - 1] + ordered[middle]) / 2.0;
}

internal sealed record NaiveSeasonRun(
    PrototypeSnapshot Snapshot,
    int TimeToProfit,
    bool Bankrupt);

internal sealed record BenchmarkRow(
    int Seed,
    string WorldHash,
    bool HasSolvencyKernel,
    int RouteCount,
    double MedianRouteProfitProxy,
    double UnmetDemandRatio,
    int TimeToProfit,
    bool Bankrupt,
    decimal CashAfter12,
    string AiMove,
    string ScenarioResult,
    int ScenarioScore,
    int ScenarioWinTick,
    int TotalCharterDeliveries,
    int DistinctDeliveredResources,
    int StableNeedTicks,
    int ActiveRouteOperations,
    int InTransitShipments,
    int RouteDispatches,
    int RouteArrivals,
    int UnitsDispatched,
    int UnitsArrived,
    int UnmetDemandServed,
    string ScriptedScenarioResult,
    int ScriptedScenarioScore,
    int ScriptedScenarioWinTick,
    int ScriptedTotalCharterDeliveries,
    int ScriptedDistinctDeliveredResources,
    int ScriptedStableNeedTicks,
    decimal ScriptedCashAfterRun,
    int ScriptedRouteDispatches,
    int ScriptedRouteArrivals,
    int ScriptedUnitsDispatched,
    int ScriptedUnitsArrived,
    int ScriptedUnmetDemandServed,
    int ScriptedFocusChanges,
    int ScriptedRouteSelections);
