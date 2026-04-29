using ChartersOfTrade.WorldGen.Core;

namespace ChartersOfTrade.Logistics.Core;

public sealed record TradeRoute(
    string Id,
    string FromNode,
    string ToNode,
    string Mode,
    int CapacityPerDay,
    int LeadDays,
    decimal CostPerUnit);

public sealed record RouteProfitEstimate(string RouteId, string ResourceId, decimal GrossMargin, decimal TransportCost, decimal NetMargin);

public static class RoutePlanner
{
    public static IReadOnlyList<TradeRoute> FromWorld(GeneratedWorld world)
    {
        return world.Edges
            .OrderBy(edge => edge.Id, StringComparer.Ordinal)
            .Select(edge => new TradeRoute(
                edge.Id.Replace("edge", "route", StringComparison.Ordinal),
                edge.FromNode,
                edge.ToNode,
                edge.Mode,
                edge.CapacityPerDay,
                Math.Max(1, (int)Math.Ceiling(edge.MovementCost / 4.0)),
                decimal.Round((decimal)edge.MovementCost * 0.12m, 2, MidpointRounding.AwayFromZero)))
            .ToArray();
    }

    public static RouteProfitEstimate EstimateProfit(
        TradeRoute route,
        string resourceId,
        decimal originPrice,
        decimal destinationPrice,
        int units)
    {
        var gross = (destinationPrice - originPrice) * units;
        var transport = route.CostPerUnit * units;
        return new RouteProfitEstimate(route.Id, resourceId, gross, transport, gross - transport);
    }
}

