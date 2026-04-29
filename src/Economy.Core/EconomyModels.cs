namespace ChartersOfTrade.Economy.Core;

public sealed record ResourceDef(
    string Id,
    string Tier,
    IReadOnlyList<string> Tags,
    decimal BasePrice,
    double Weight,
    int SpoilDays,
    IReadOnlyList<string> Substitutes);

public sealed record ResourceAmount(string ResourceId, int Amount);

public sealed record RecipeDef(
    string Id,
    string BuildingType,
    IReadOnlyList<ResourceAmount> Inputs,
    IReadOnlyList<ResourceAmount> Outputs,
    WorkforceRequirement Workforce,
    int BaseDays,
    string RequiresTech);

public sealed record WorkforceRequirement(int Peasants, int Artisans);

public sealed record MarketNeed(string ResourceId, int DesiredStock, int ConsumptionPerTick);

public sealed record MarketPrice(string ResourceId, decimal Price, double Scarcity);

public sealed record ProductionResult(string RecipeId, bool Produced, string Reason);

