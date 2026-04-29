using ChartersOfTrade.Economy.Core;

namespace ChartersOfTrade.CitySim.Core;

public enum CityLevel
{
    Hamlet = 0,
    Borough = 1,
    City = 2
}

public sealed record PopulationCohorts(int Peasants, int Artisans, int Merchants, int Elite)
{
    public int Total => Peasants + Artisans + Merchants + Elite;
}

public sealed record CityState(
    string Id,
    CityLevel Level,
    PopulationCohorts Population,
    IReadOnlyList<string> Districts,
    Inventory Market,
    Inventory CompanyWarehouse);

public sealed record CityGrowthReport(string CityId, double SupplySatisfaction, int PopulationDelta, CityLevel Level);

public sealed class CityGrowthSystem
{
    public CityGrowthReport Evaluate(CityState city, IEnumerable<MarketNeed> needs)
    {
        var needsList = needs.ToArray();
        if (needsList.Length == 0)
        {
            return new CityGrowthReport(city.Id, 1.0, 0, city.Level);
        }

        var total = 0.0;
        foreach (var need in needsList)
        {
            total += Math.Clamp(city.Market.Get(need.ResourceId) / (double)Math.Max(1, need.DesiredStock), 0, 1);
        }

        var satisfaction = total / needsList.Length;
        var delta = satisfaction switch
        {
            >= 0.90 => 3,
            >= 0.65 => 1,
            <= 0.25 => -3,
            <= 0.45 => -1,
            _ => 0
        };

        var nextPopulation = Math.Max(0, city.Population.Total + delta);
        var nextLevel = nextPopulation >= 500 ? CityLevel.City : nextPopulation >= 150 ? CityLevel.Borough : CityLevel.Hamlet;
        return new CityGrowthReport(city.Id, Math.Round(satisfaction, 4), delta, nextLevel);
    }
}

