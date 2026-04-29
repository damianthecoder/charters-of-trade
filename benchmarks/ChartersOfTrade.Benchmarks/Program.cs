using System.Globalization;
using ChartersOfTrade.GodotBridge;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var seeds = Enumerable.Range(1000, 25).ToArray();
var bridge = new SimulationBridge();
var rows = new List<BenchmarkRow>();

foreach (var seed in seeds)
{
    var session = bridge.CreatePrototypeSession(seed);
    var initial = session.Current;
    var timeToProfit = -1;
    var bankrupt = false;

    for (var tick = 1; tick <= 12; tick++)
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

    var snapshot = session.Current;
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
        timeToProfit,
        bankrupt,
        snapshot.Company.Cash,
        snapshot.AiChoice.OpportunityId));
}

Console.WriteLine("seed,world_hash,solvency_kernel,route_count,median_route_profit_proxy,unmet_demand_ratio,time_to_profit,bankrupt,cash_after_12,ai_move");
foreach (var row in rows)
{
    Console.WriteLine($"{row.Seed},{row.WorldHash},{row.HasSolvencyKernel},{row.RouteCount},{row.MedianRouteProfitProxy},{row.UnmetDemandRatio},{row.TimeToProfit},{row.Bankrupt},{row.CashAfter12},{row.AiMove}");
}

var playable = rows.Count(row => row.HasSolvencyKernel);
Console.WriteLine();
Console.WriteLine($"Playable seeds: {playable}/{rows.Count}");
Console.WriteLine($"Average unmet demand ratio: {rows.Average(row => row.UnmetDemandRatio):0.0000}");
Console.WriteLine($"Median time to profit: {Median(rows.Where(row => row.TimeToProfit > 0).Select(row => row.TimeToProfit)):0.0}");
Console.WriteLine($"Bankruptcy frequency: {rows.Count(row => row.Bankrupt)}/{rows.Count}");

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
    string AiMove);
