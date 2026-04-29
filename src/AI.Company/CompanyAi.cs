using ChartersOfTrade.Logistics.Core;

namespace ChartersOfTrade.AI.Company;

public sealed record Opportunity(
    string Id,
    TradeRoute Route,
    string ResourceId,
    decimal ExpectedRevenue,
    decimal CapitalCost,
    double TransportRisk,
    double MarketVolatility,
    decimal StrategicBonus);

public sealed record OpportunityScore(string OpportunityId, decimal Score);

public sealed class CompanyUtilityAi
{
    public OpportunityScore Score(Opportunity opportunity)
    {
        var riskPenalty = (decimal)(opportunity.TransportRisk + opportunity.MarketVolatility) * 10m;
        var score = opportunity.ExpectedRevenue - opportunity.CapitalCost - riskPenalty + opportunity.StrategicBonus;
        return new OpportunityScore(opportunity.Id, decimal.Round(score, 2, MidpointRounding.AwayFromZero));
    }

    public OpportunityScore ChooseBest(IEnumerable<Opportunity> opportunities)
    {
        var scored = opportunities.Select(Score).OrderByDescending(score => score.Score).ThenBy(score => score.OpportunityId, StringComparer.Ordinal).ToArray();
        if (scored.Length == 0)
        {
            throw new ArgumentException("At least one opportunity is required.", nameof(opportunities));
        }

        return scored[0];
    }
}

