using ChartersOfTrade.Persistence.Core;

namespace ChartersOfTrade.GodotBridge;

public static class FirstCharterSeason
{
    public const string ScenarioId = "first_charter_season";
    public const string Label = "First Charter Season";
    public const int RulesVersion = 1;
    public const int TickLimit = 12;
    public const decimal CashTarget = 1250m;
    public const decimal BankruptcyFloor = 0m;
    public const int RequiredCharterDeliveries = 3;
    public const int RequiredDistinctResources = 2;
    public const int RequiredStableNeeds = 4;
    public const int StabilityWindowTicks = 3;
    public const string InProgress = "in_progress";
    public const string Won = "won";
    public const string Bankrupt = "bankrupt";
    public const string Timeout = "timeout";

    public static ScenarioObjectiveSaveState CreateInitialState(decimal initialCash)
    {
        return new ScenarioObjectiveSaveState(
            ScenarioId,
            RulesVersion,
            StartedTick: 0,
            CurrentTick: 0,
            EndTick: null,
            InProgress,
            [],
            [],
            new Dictionary<string, int>(StringComparer.Ordinal),
            initialCash,
            FinalScore: 0);
    }

    public static int Score(decimal cash, int completedCharters, int distinctResources, int stableNeeds)
    {
        var cashScore = Ratio(cash, CashTarget) * 35m;
        var charterScore = Ratio(completedCharters, RequiredCharterDeliveries) * 20m;
        var varietyScore = Ratio(distinctResources, RequiredDistinctResources) * 15m;
        var stabilityScore = Ratio(stableNeeds, RequiredStableNeeds) * 25m;
        var solvencyScore = cash >= BankruptcyFloor ? 5m : 0m;
        return (int)Math.Round(Math.Clamp(cashScore + charterScore + varietyScore + stabilityScore + solvencyScore, 0m, 100m), MidpointRounding.AwayFromZero);
    }

    public static string ResolveEndReason(decimal cash, int tick, int completedCharters, int distinctResources, int stableNeeds)
    {
        if (cash < BankruptcyFloor)
        {
            return Bankrupt;
        }

        if (cash >= CashTarget
            && completedCharters >= RequiredCharterDeliveries
            && distinctResources >= RequiredDistinctResources
            && stableNeeds >= RequiredStableNeeds)
        {
            return Won;
        }

        return tick >= TickLimit ? Timeout : InProgress;
    }

    private static decimal Ratio(decimal value, decimal target)
    {
        return target <= 0m ? 1m : Math.Clamp(value / target, 0m, 1m);
    }
}
