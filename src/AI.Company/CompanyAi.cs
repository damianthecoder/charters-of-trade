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

public sealed record NpcPressureCandidate(
    string Id,
    string Intent,
    string CityId,
    string? RouteId,
    string? RouteOperationId,
    string? ProductionOpportunityId,
    string ResourceId,
    decimal ExpectedValue,
    int ShipmentPriority,
    int DemandServed,
    bool CanContest,
    decimal StrategicBonus,
    string Reason);

public sealed record NpcPressureScore(
    string CandidateId,
    string CompanyId,
    string Intent,
    decimal Pressure,
    int ShipmentPriority,
    decimal ExpectedValue,
    string Reason);

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

public sealed class DeterministicNpcPressureAi
{
    public NpcPressureScore Score(string companyId, NpcPressureCandidate candidate)
    {
        if (!candidate.CanContest)
        {
            return new NpcPressureScore(
                candidate.Id,
                companyId,
                candidate.Intent,
                0m,
                candidate.ShipmentPriority,
                candidate.ExpectedValue,
                candidate.Reason);
        }

        const decimal readiness = 12m;
        var demand = candidate.DemandServed * 1.5m;
        var priority = candidate.ShipmentPriority * 8m;
        var pressure = decimal.Round(
            Math.Max(0m, candidate.ExpectedValue + priority + demand + readiness + candidate.StrategicBonus),
            2,
            MidpointRounding.AwayFromZero);

        return new NpcPressureScore(
            candidate.Id,
            companyId,
            candidate.Intent,
            pressure,
            candidate.ShipmentPriority,
            candidate.ExpectedValue,
            candidate.Reason);
    }

    public IReadOnlyList<NpcPressureScore> Rank(string companyId, IEnumerable<NpcPressureCandidate> candidates)
    {
        return candidates
            .Select(candidate => Score(companyId, candidate))
            .OrderByDescending(score => score.Pressure)
            .ThenByDescending(score => score.ShipmentPriority)
            .ThenByDescending(score => score.ExpectedValue)
            .ThenBy(score => score.CompanyId, StringComparer.Ordinal)
            .ThenBy(score => score.Intent, StringComparer.Ordinal)
            .ThenBy(score => score.CandidateId, StringComparer.Ordinal)
            .ToArray();
    }
}
