namespace ChartersOfTrade.Economy.Core;

public sealed class EconomyTick
{
    public IReadOnlyList<MarketPrice> CalculatePrices(
        IEnumerable<ResourceDef> resources,
        Inventory market,
        IEnumerable<MarketNeed> needs)
    {
        var needsByResource = needs.ToDictionary(need => need.ResourceId, StringComparer.Ordinal);
        var prices = new List<MarketPrice>();

        foreach (var resource in resources.OrderBy(resource => resource.Id, StringComparer.Ordinal))
        {
            needsByResource.TryGetValue(resource.Id, out var need);
            var desired = Math.Max(1, need?.DesiredStock ?? 10);
            var consumption = Math.Max(0, need?.ConsumptionPerTick ?? 0);
            var stock = market.Get(resource.Id);
            var scarcity = Math.Clamp((desired - stock) / (double)desired, -1.00, 1.75);
            var coverage = consumption <= 0 ? double.PositiveInfinity : stock / (double)consumption;
            var stockoutPremium = stock == 0 && consumption > 0 ? 0.35 : 0.0;
            var immediatePressure = consumption > 0 && coverage < 2.0
                ? Math.Min(0.24, (2.0 - coverage) * 0.12)
                : 0.0;
            var surplusDiscount = stock > desired
                ? Math.Min(0.25, ((stock - desired) / (double)desired) * 0.18)
                : 0.0;
            var perishabilityDiscount = resource.SpoilDays > 0 && stock > desired
                ? Math.Min(0.12, ((stock - desired) / (double)desired) * 0.08)
                : 0.0;
            var multiplier = 1.0
                + Math.Max(0.0, scarcity) * 0.55
                + Math.Min(0.0, scarcity) * 0.35
                + stockoutPremium
                + immediatePressure
                - surplusDiscount
                - perishabilityDiscount;
            multiplier = Math.Clamp(multiplier, 0.55, 2.25);
            var price = decimal.Round(resource.BasePrice * (decimal)multiplier, 2, MidpointRounding.AwayFromZero);
            prices.Add(new MarketPrice(resource.Id, Math.Max(1m, price), Math.Round(scarcity, 4)));
        }

        return prices;
    }

    public IReadOnlyList<ProductionResult> RunProduction(Inventory inventory, IEnumerable<RecipeDef> recipes)
    {
        var results = new List<ProductionResult>();

        foreach (var recipe in recipes.OrderBy(recipe => recipe.Id, StringComparer.Ordinal))
        {
            if (recipe.Inputs.Any(input => inventory.Get(input.ResourceId) < input.Amount))
            {
                results.Add(new ProductionResult(recipe.Id, false, "missing_input"));
                continue;
            }

            foreach (var input in recipe.Inputs)
            {
                inventory.TryRemove(input.ResourceId, input.Amount);
            }

            foreach (var output in recipe.Outputs)
            {
                inventory.Add(output.ResourceId, output.Amount);
            }

            results.Add(new ProductionResult(recipe.Id, true, "ok"));
        }

        return results;
    }
}
