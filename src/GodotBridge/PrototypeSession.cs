using System.Globalization;
using ChartersOfTrade.AI.Company;
using ChartersOfTrade.CitySim.Core;
using ChartersOfTrade.Content.Core;
using ChartersOfTrade.Economy.Core;
using ChartersOfTrade.Logistics.Core;
using ChartersOfTrade.Persistence.Core;
using ChartersOfTrade.WorldGen.Core;

namespace ChartersOfTrade.GodotBridge;

public sealed record PrototypeCityView(
    string Id,
    string Name,
    int X,
    int Y,
    CityLevel Level,
    int Population,
    IReadOnlyList<string> Districts,
    PrototypeCitySpecialization Specialization,
    double SupplySatisfaction,
    IReadOnlyDictionary<string, int> MarketStock,
    IReadOnlyDictionary<string, int> CompanyWarehouse,
    IReadOnlyList<PrototypeMarketSignal> MarketSignals);

public sealed record PrototypeCitySpecialization(
    string RoleId,
    string Label,
    IReadOnlyList<string> AnchorResources,
    IReadOnlyList<string> OutputResources,
    string Rationale);

public sealed record PrototypeMarketSignal(
    string ResourceId,
    decimal Price,
    double Scarcity,
    int MarketStock,
    int WarehouseStock,
    int DesiredStock,
    int ConsumptionPerTick,
    int SafetyStock,
    int ReorderPoint,
    bool IsPolicyOverridden,
    string PolicyMode,
    int ShipmentPriority,
    string Reason,
    string PolicyAction);

public sealed record PrototypeLedgerEntry(
    int Tick,
    string Category,
    string Message,
    decimal CashDelta,
    string RelatedId);

public sealed record PrototypeRouteContractView(
    string Id,
    string RouteId,
    string FromNode,
    string ToNode,
    string ResourceId,
    decimal ExpectedRevenue,
    decimal TransportCost,
    decimal ExpectedNet,
    int CapacityPerDay,
    int Units,
    int ShipmentPriority,
    string PolicyAction);

public sealed record PrototypeRouteOperationView(
    string Id,
    string SourceContractId,
    string RouteId,
    string FromNode,
    string ToNode,
    string ResourceId,
    bool IsActive,
    bool CanDispatch,
    int CapacityPerDay,
    int ExpectedUnits,
    int UsedCapacity,
    int FreeCapacity,
    int ShipmentPriority,
    decimal ExpectedRevenue,
    decimal TransportCost,
    decimal ExpectedNet,
    int UnmetDemandServed,
    string Status,
    string PausedReason,
    string PolicyAction);

public sealed record PrototypeRoutePolicyView(
    string RouteId,
    IReadOnlyList<string> ReservedResources,
    string? PriorityResourceId);

public sealed record PrototypeProductionResourceLineView(
    string ResourceId,
    int RequiredAmount,
    int OutputAmount,
    int WarehouseStock,
    int MarketStock,
    int ProtectedStock,
    int AvailableAmount,
    int MissingAmount,
    decimal LocalUnitPrice,
    double LocalScarcity,
    string? BestDestinationCityId,
    string? BestRouteId,
    decimal? BestDestinationUnitPrice,
    int DestinationShipmentPriority);

public sealed record PrototypeProductionChainOpportunityView(
    string Id,
    string CityId,
    string CityName,
    string RecipeId,
    string BuildingType,
    IReadOnlyList<PrototypeProductionResourceLineView> Inputs,
    IReadOnlyList<PrototypeProductionResourceLineView> Outputs,
    int MaxRunsFromWarehouse,
    decimal InputCost,
    decimal OutputValue,
    decimal ExpectedMargin,
    string? BottleneckResourceId,
    int MissingInputUnits,
    int DestinationShipmentPriority,
    decimal Score,
    bool IsReady,
    string? CandidateRouteId,
    string Reason);

public sealed record PrototypeNpcPressureView(
    string Id,
    string CompanyId,
    string CompanyName,
    string Intent,
    string CityId,
    string CityName,
    string? TargetCityId,
    string? TargetCityName,
    string? RouteId,
    string? RouteOperationId,
    string? ProductionOpportunityId,
    string ResourceId,
    decimal Pressure,
    int ShipmentPriority,
    decimal ExpectedValue,
    bool CanContest,
    string Reason);

public sealed record PrototypeSnapshot(
    int Tick,
    GeneratedWorld World,
    string ContentHash,
    IReadOnlyList<ResourceDef> Resources,
    IReadOnlyList<TradeRoute> Routes,
    IReadOnlyList<MarketPrice> Prices,
    IReadOnlyList<PrototypeCityView> Cities,
    CompanyState Company,
    CalendarState Calendar,
    OpportunityScore AiChoice,
    IReadOnlyList<PrototypeLedgerEntry> Ledger,
    string SaveHash)
{
    public IReadOnlyList<PrototypeRouteContractView> AvailableContracts { get; init; } = [];

    public string? SelectedContractId { get; init; }

    public IReadOnlyList<PrototypeRouteOperationView> RouteOperationCandidates { get; init; } = [];

    public PrototypeRouteOperationView? ActiveRouteOperation { get; init; }

    public IReadOnlyList<PrototypeRoutePolicyView> RoutePolicies { get; init; } = [];

    public IReadOnlyList<PrototypeProductionChainOpportunityView> ProductionChainOpportunities { get; init; } = [];

    public IReadOnlyList<PrototypeNpcPressureView> NpcPressures { get; init; } = [];

    public double UnmetDemandRatio => Prices.Count == 0 ? 0 : Math.Round(Prices.Average(price => Math.Max(0, price.Scarcity)), 4);

    public decimal LastTickCashDelta => Ledger.Where(entry => entry.Tick == Tick).Sum(entry => entry.CashDelta);
}

public sealed class PrototypeSession
{
    private readonly GameContent _content;
    private readonly IReadOnlyList<MarketNeed> _needs;
    private readonly EconomyTick _economy = new();
    private readonly CityGrowthSystem _cityGrowth = new();
    private readonly List<RuntimeCity> _cities;
    private readonly List<PrototypeLedgerEntry> _ledger = [];
    private readonly Dictionary<WarehousePolicyKey, WarehousePolicyOverride> _warehousePolicyOverrides = [];
    private readonly Dictionary<string, RoutePolicy> _routePolicies = new(StringComparer.Ordinal);

    private CompanyState _company = new(1000m, 0m, 50, "merchant_league");
    private CalendarState _calendar = new(1, 1);
    private OpportunityScore _aiChoice = new("none", 0m);
    private string? _selectedContractId;

    private const int MinPolicyStock = 0;
    private const int MaxPolicyStock = 64;
    private const string NpcCompanyId = "north_sea_company";
    private const string NpcCompanyName = "North Sea Company";
    public const string BalancedWarehouseMode = "balanced";
    public const string ConservativeWarehouseMode = "conservative";

    public PrototypeSession(GeneratedWorld world, GameContent content, IReadOnlyList<TradeRoute> routes)
    {
        World = world;
        _content = content;
        Routes = routes;
        _needs = StarterScenarioFactory.CreateNeeds(content.Resources);
        _cities = world.Nodes.OrderBy(node => node.Id, StringComparer.Ordinal).Select(CreateCity).ToList();
        InitializeRoutePolicies();
        Current = BuildSnapshot(0);
    }

    public GeneratedWorld World { get; }

    public IReadOnlyList<TradeRoute> Routes { get; }

    public PrototypeSnapshot Current { get; private set; }

    public bool SelectRouteContract(string contractId)
    {
        if (string.IsNullOrWhiteSpace(contractId))
        {
            return false;
        }

        var contracts = BuildAvailableContracts();
        if (contracts.All(contract => contract.Id != contractId))
        {
            return false;
        }

        _selectedContractId = contractId;
        Current = BuildSnapshot(Current.Tick);
        return true;
    }

    public bool ClearRouteOperation()
    {
        if (_selectedContractId is null)
        {
            return false;
        }

        _selectedContractId = null;
        Current = BuildSnapshot(Current.Tick);
        return true;
    }

    public bool SetWarehousePolicy(string cityId, string resourceId, int safetyStock, int reorderPoint, string? mode = null)
    {
        if (!IsKnownPolicyTarget(cityId, resourceId))
        {
            return false;
        }

        var city = _cities.First(item => string.Equals(item.Id, cityId, StringComparison.Ordinal));
        var currentPolicy = EffectivePolicyFor(city, resourceId, NeedFor(resourceId));
        if (!TryNormalizeWarehouseMode(mode ?? currentPolicy.Mode, out var normalizedMode))
        {
            return false;
        }

        var clampedSafetyStock = Math.Clamp(safetyStock, MinPolicyStock, MaxPolicyStock);
        var clampedReorderPoint = Math.Clamp(reorderPoint, MinPolicyStock, MaxPolicyStock);
        clampedReorderPoint = Math.Max(clampedReorderPoint, clampedSafetyStock);

        _warehousePolicyOverrides[new WarehousePolicyKey(cityId, resourceId)] = new WarehousePolicyOverride(clampedSafetyStock, clampedReorderPoint, normalizedMode);
        Current = BuildSnapshot(Current.Tick);
        return true;
    }

    public bool SetWarehousePolicyMode(string cityId, string resourceId, string mode)
    {
        if (!IsKnownPolicyTarget(cityId, resourceId) || !TryNormalizeWarehouseMode(mode, out var normalizedMode))
        {
            return false;
        }

        var key = new WarehousePolicyKey(cityId, resourceId);
        var need = NeedFor(resourceId);
        if (string.Equals(normalizedMode, BalancedWarehouseMode, StringComparison.Ordinal))
        {
            _warehousePolicyOverrides.Remove(key);
            Current = BuildSnapshot(Current.Tick);
            return true;
        }

        var policy = PolicyForMode(need, normalizedMode, true);
        _warehousePolicyOverrides[key] = new WarehousePolicyOverride(policy.SafetyStock, policy.ReorderPoint, normalizedMode);
        Current = BuildSnapshot(Current.Tick);
        return true;
    }

    public bool SetRouteResourceReservation(string routeId, string resourceId, bool reserved)
    {
        if (!TryGetRoutePolicy(routeId, out var policy) || !IsKnownRouteResource(resourceId))
        {
            return false;
        }

        var reservedResources = policy.ReservedResources.ToHashSet(StringComparer.Ordinal);
        if (reserved)
        {
            reservedResources.Add(resourceId);
        }
        else
        {
            reservedResources.Remove(resourceId);
        }

        var priorityResourceId = policy.PriorityResourceId;
        if (priorityResourceId is not null && !reservedResources.Contains(priorityResourceId))
        {
            priorityResourceId = null;
        }

        _routePolicies[routeId] = new RoutePolicy(
            routeId,
            reservedResources.Order(StringComparer.Ordinal).ToArray(),
            priorityResourceId);
        Current = BuildSnapshot(Current.Tick);
        return true;
    }

    public bool SetRoutePriorityResource(string routeId, string? resourceId)
    {
        if (!TryGetRoutePolicy(routeId, out var policy))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(resourceId))
        {
            _routePolicies[routeId] = policy with { PriorityResourceId = null };
            Current = BuildSnapshot(Current.Tick);
            return true;
        }

        if (!IsKnownRouteResource(resourceId))
        {
            return false;
        }

        var reservedResources = policy.ReservedResources.ToHashSet(StringComparer.Ordinal);
        reservedResources.Add(resourceId);
        _routePolicies[routeId] = new RoutePolicy(
            routeId,
            reservedResources.Order(StringComparer.Ordinal).ToArray(),
            resourceId);
        Current = BuildSnapshot(Current.Tick);
        return true;
    }

    public PrototypeSnapshot AdvanceTick()
    {
        var nextTick = Current.Tick + 1;
        _calendar = _calendar with { DayOfYear = _calendar.DayOfYear + 1 };
        var selectedOperation = SelectedActiveRouteOperation();
        var reservation = ReservationFor(selectedOperation);

        var productionCash = RunProduction(nextTick, reservation);
        var logisticsCash = RunLogistics(nextTick);
        var aiCash = RunAi(nextTick);
        RunCityGrowth(nextTick);

        var cashDelta = productionCash + logisticsCash + aiCash;
        _company = _company with { Cash = decimal.Round(_company.Cash + cashDelta, 2, MidpointRounding.AwayFromZero) };

        Current = BuildSnapshot(nextTick);
        return Current;
    }

    private RuntimeCity CreateCity(WorldNode node, int index)
    {
        var market = StarterScenarioFactory.CreateInitialMarket(_content.Resources);
        var warehouse = new Inventory();

        foreach (var resourceId in node.Resources.Order(StringComparer.Ordinal))
        {
            warehouse.Add(resourceId, 8);
            market.Add(resourceId, 4);
        }

        var population = index switch
        {
            0 => new PopulationCohorts(90, 12, 4, 0),
            <= 2 => new PopulationCohorts(70, 8, 2, 0),
            _ => new PopulationCohorts(52, 4, 1, 0)
        };

        return new RuntimeCity(
            node.Id,
            NameFor(node, index),
            node.X,
            node.Y,
            CityLevel.Hamlet,
            population,
            ["market", index == 0 ? "charter_house" : "trading_post"],
            SpecializationFor(node),
            market,
            warehouse,
            1.0);
    }

    private void InitializeRoutePolicies()
    {
        var reservedResources = _needs
            .Select(need => need.ResourceId)
            .Order(StringComparer.Ordinal)
            .ToArray();

        foreach (var route in Routes.OrderBy(route => route.Id, StringComparer.Ordinal))
        {
            _routePolicies[route.Id] = new RoutePolicy(route.Id, reservedResources, null);
        }
    }

    private decimal RunProduction(int tick, ProductionReservation? reservation)
    {
        var cash = 0m;
        foreach (var city in _cities.OrderBy(city => city.Id, StringComparer.Ordinal))
        {
            var producedIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var recipe in _content.Recipes.OrderBy(recipe => recipe.Id, StringComparer.Ordinal))
            {
                if (!CanRunInCity(city, recipe, reservation))
                {
                    continue;
                }

                foreach (var input in recipe.Inputs)
                {
                    city.CompanyWarehouse.TryRemove(input.ResourceId, input.Amount);
                }

                foreach (var output in recipe.Outputs)
                {
                    city.CompanyWarehouse.Add(output.ResourceId, output.Amount);
                }

                producedIds.Add(recipe.Id);
            }

            if (producedIds.Count == 0)
            {
                continue;
            }

            var cityCash = 0m;
            foreach (var recipe in _content.Recipes.Where(recipe => producedIds.Contains(recipe.Id)))
            {
                foreach (var output in recipe.Outputs)
                {
                    var moved = Math.Min(output.Amount, city.CompanyWarehouse.Get(output.ResourceId));
                    if (moved <= 0)
                    {
                        continue;
                    }

                    city.CompanyWarehouse.TryRemove(output.ResourceId, moved);
                    city.Market.Add(output.ResourceId, moved);
                    cityCash += PriceFor(city, output.ResourceId) * moved * 0.05m;
                }
            }

            cityCash = RoundMoney(cityCash);
            cash += cityCash;
            _ledger.Add(new PrototypeLedgerEntry(tick, "Production", $"{city.Name}: {producedIds.Count} recipes produced", cityCash, city.Id));
        }

        return cash;
    }

    private decimal RunLogistics(int tick)
    {
        var selectedOperation = SelectedActiveRouteOperation();
        if (selectedOperation is not null)
        {
            return ExecuteRouteOperation(tick, selectedOperation);
        }

        if (_selectedContractId is not null)
        {
            _selectedContractId = null;
        }

        var charter = _cities[0];
        var prices = PricesFor(charter);
        var needsByResource = _needs.ToDictionary(need => need.ResourceId, StringComparer.Ordinal);
        var cash = 0m;

        foreach (var route in Routes.Where(route => route.FromNode == charter.Id || route.ToNode == charter.Id).OrderBy(route => route.Id, StringComparer.Ordinal).Take(3))
        {
            var otherId = route.FromNode == charter.Id ? route.ToNode : route.FromNode;
            var source = _cities.First(city => city.Id == otherId);
            var candidates = source.CompanyWarehouse.Stock
                .Select(kvp => new
                {
                    kvp.Key,
                    Exportable = ExportableWarehouseUnits(source, kvp.Key),
                    Priority = ShipmentPriority(charter, kvp.Key) + RoutePriorityBoost(route.Id, kvp.Key)
                })
                .Where(kvp => kvp.Exportable > 0
                    && needsByResource.ContainsKey(kvp.Key)
                    && RouteAllowsResource(route.Id, kvp.Key))
                .OrderByDescending(kvp => kvp.Priority)
                .ThenByDescending(kvp => NeedScarcity(charter, needsByResource[kvp.Key]))
                .ThenBy(kvp => kvp.Key, StringComparer.Ordinal)
                .ToArray();

            if (candidates.Length == 0)
            {
                continue;
            }

            var candidate = candidates[0];
            var units = Math.Min(Math.Min(candidate.Exportable, 3), Math.Max(1, route.CapacityPerDay / 4));
            if (!source.CompanyWarehouse.TryRemove(candidate.Key, units))
            {
                continue;
            }

            charter.Market.Add(candidate.Key, units);
            prices.TryGetValue(candidate.Key, out var price);
            var revenue = price * units;
            var transportCost = route.CostPerUnit * units;
            var net = RoundMoney(revenue - transportCost);
            cash += net;

            _ledger.Add(new PrototypeLedgerEntry(tick, "Logistics", $"{route.Id}: delivered {units} {candidate.Key} from {source.Name}", net, route.Id));
        }

        return cash;
    }

    private decimal ExecuteRouteOperation(int tick, PrototypeRouteOperationView operation)
    {
        var route = Routes.FirstOrDefault(route => route.Id == operation.RouteId);
        var source = _cities.FirstOrDefault(city => city.Id == operation.FromNode);
        var destination = _cities.FirstOrDefault(city => city.Id == operation.ToNode);
        if (route is null || source is null || destination is null)
        {
            return 0m;
        }

        if (!operation.CanDispatch)
        {
            _ledger.Add(new PrototypeLedgerEntry(
                tick,
                "Logistics",
                $"{route.Id}: route operation paused for {operation.ResourceId}: {operation.PausedReason}",
                0m,
                route.Id));
            return 0m;
        }

        var demandGap = DemandGap(destination, operation.ResourceId);
        var units = Math.Min(
            operation.ExpectedUnits,
            Math.Min(ExportableWarehouseUnits(source, operation.ResourceId), Math.Min(route.CapacityPerDay, demandGap)));
        if (units <= 0 || !source.CompanyWarehouse.TryRemove(operation.ResourceId, units))
        {
            _ledger.Add(new PrototypeLedgerEntry(
                tick,
                "Logistics",
                $"{route.Id}: route operation paused for {operation.ResourceId}: no exportable stock",
                0m,
                route.Id));
            return 0m;
        }

        var prices = PricesFor(destination);
        prices.TryGetValue(operation.ResourceId, out var price);
        destination.Market.Add(operation.ResourceId, units);

        var revenue = price * units;
        var transportCost = route.CostPerUnit * units;
        var net = RoundMoney(revenue - transportCost);
        var unmetServed = Math.Min(units, demandGap);
        _ledger.Add(new PrototypeLedgerEntry(
            tick,
            "Logistics",
            $"{route.Id}: route operation delivered {units} {operation.ResourceId} from {source.Name} to {destination.Name}, served {unmetServed} unmet demand",
            net,
            route.Id));
        return net;
    }

    private decimal RunAi(int tick)
    {
        var contracts = BuildAvailableContracts();
        var routeOperations = BuildRouteOperationCandidates(contracts);
        var activeOperation = BuildActiveRouteOperation();
        var productionChains = BuildProductionChainOpportunities();
        var pressures = BuildNpcPressures(productionChains, routeOperations, activeOperation);
        var topPressure = pressures.FirstOrDefault();

        if (topPressure is null)
        {
            _aiChoice = new OpportunityScore("none", 0m);
            return 0m;
        }

        _aiChoice = new OpportunityScore(topPressure.Id, topPressure.Pressure);
        var pressure = topPressure.Pressure > 0 ? RoundMoney(Math.Min(8m, topPressure.Pressure * 0.02m)) : 0m;
        _ledger.Add(new PrototypeLedgerEntry(
            tick,
            "AI",
            $"{topPressure.CompanyName} {NpcIntentLabel(topPressure.Intent)} {topPressure.ResourceId} at {topPressure.CityName}; pressure {topPressure.Pressure.ToString("0.00", CultureInfo.InvariantCulture)}; {topPressure.Reason}",
            -pressure,
            topPressure.RouteId ?? topPressure.CityId));
        return -pressure;
    }

    private void RunCityGrowth(int tick)
    {
        foreach (var city in _cities.OrderBy(city => city.Id, StringComparer.Ordinal))
        {
            ConsumeNeeds(city.Market, _needs);
            var state = new CityState(city.Id, city.Level, city.Population, city.Districts, city.Market, city.CompanyWarehouse);
            var report = _cityGrowth.Evaluate(state, _needs);
            city.SupplySatisfaction = report.SupplySatisfaction;
            city.Level = report.Level;
            city.Population = ApplyPopulationDelta(city.Population, report.PopulationDelta);

            if (report.PopulationDelta != 0)
            {
                _ledger.Add(new PrototypeLedgerEntry(tick, "City", $"{city.Name}: population {Signed(report.PopulationDelta)}", 0m, city.Id));
            }
        }
    }

    private PrototypeSnapshot BuildSnapshot(int tick)
    {
        var charter = _cities[0];
        var prices = _economy.CalculatePrices(_content.Resources, charter.Market, _needs);
        var contracts = BuildAvailableContracts();
        var operationCandidates = BuildRouteOperationCandidates(contracts);
        var activeOperation = BuildActiveRouteOperation();
        var productionChains = BuildProductionChainOpportunities();
        var npcPressures = BuildNpcPressures(productionChains, operationCandidates, activeOperation);
        var selectedContractId = activeOperation is not null
            ? _selectedContractId
            : null;
        _selectedContractId = selectedContractId;
        var save = BuildSave(prices, selectedContractId);
        var views = _cities
            .OrderBy(city => city.Id, StringComparer.Ordinal)
            .Select(city => new PrototypeCityView(
                city.Id,
                city.Name,
                city.X,
                city.Y,
                city.Level,
                city.Population.Total,
                ReadOnlyCopy(city.Districts),
                CopySpecialization(city.Specialization),
                city.SupplySatisfaction,
                city.Market.ToDictionary(),
                city.CompanyWarehouse.ToDictionary(),
                BuildMarketSignals(city)))
            .ToArray();

        return new PrototypeSnapshot(
            tick,
            World,
            _content.ContentHash,
            _content.Resources,
            Routes,
            prices,
            views,
            _company,
            _calendar,
            _aiChoice,
            _ledger.ToArray(),
            SaveCodec.ComputeStateHash(save))
        {
            AvailableContracts = contracts,
            SelectedContractId = selectedContractId,
            RouteOperationCandidates = operationCandidates,
            ActiveRouteOperation = activeOperation,
            RoutePolicies = BuildRoutePolicyViews(),
            ProductionChainOpportunities = productionChains,
            NpcPressures = npcPressures
        };
    }

    private IReadOnlyList<PrototypeRouteContractView> BuildAvailableContracts()
    {
        var charter = _cities[0];
        var needsByResource = _needs.ToDictionary(need => need.ResourceId, StringComparer.Ordinal);
        var prices = PricesFor(charter);
        var contracts = new List<PrototypeRouteContractView>();

        foreach (var route in Routes
            .Where(route => route.FromNode == charter.Id || route.ToNode == charter.Id)
            .OrderBy(route => route.Id, StringComparer.Ordinal))
        {
            var sourceId = route.FromNode == charter.Id ? route.ToNode : route.FromNode;
            var source = _cities.FirstOrDefault(city => city.Id == sourceId);
            if (source is null)
            {
                continue;
            }

            foreach (var stock in source.CompanyWarehouse.Stock
                .Where(kvp => kvp.Value > 0 && needsByResource.ContainsKey(kvp.Key) && RouteAllowsResource(route.Id, kvp.Key))
                .OrderBy(kvp => kvp.Key, StringComparer.Ordinal))
            {
                var units = Math.Min(ExportableWarehouseUnits(source, stock.Key), ContractUnits(route));
                if (units <= 0)
                {
                    continue;
                }

                prices.TryGetValue(stock.Key, out var price);
                var priority = ShipmentPriority(charter, stock.Key) + RoutePriorityBoost(route.Id, stock.Key);
                var expectedRevenue = decimal.Round(price * units, 2, MidpointRounding.AwayFromZero);
                var transportCost = decimal.Round(route.CostPerUnit * units, 2, MidpointRounding.AwayFromZero);
                contracts.Add(new PrototypeRouteContractView(
                    RouteContractId(route.Id, stock.Key),
                    route.Id,
                    source.Id,
                    charter.Id,
                    stock.Key,
                    expectedRevenue,
                    transportCost,
                    expectedRevenue - transportCost,
                    route.CapacityPerDay,
                    units,
                    priority,
                    ContractPolicyAction(route.Id, charter, stock.Key)));
            }
        }

        return contracts
            .OrderByDescending(contract => contract.ShipmentPriority)
            .ThenByDescending(contract => contract.ExpectedNet)
            .ThenBy(contract => contract.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private IReadOnlyList<PrototypeRouteOperationView> BuildRouteOperationCandidates(IReadOnlyList<PrototypeRouteContractView> contracts)
    {
        return ReadOnlyCopy(contracts
            .Select(contract => BuildRouteOperationFromContract(contract, string.Equals(contract.Id, _selectedContractId, StringComparison.Ordinal)))
            .Where(operation => operation is not null)
            .Select(operation => operation!)
            .OrderByDescending(operation => operation.ShipmentPriority)
            .ThenByDescending(operation => operation.ExpectedNet)
            .ThenBy(operation => operation.Id, StringComparer.Ordinal)
            .ToArray());
    }

    private PrototypeRouteOperationView? SelectedActiveRouteOperation()
    {
        return _selectedContractId is null ? null : BuildActiveRouteOperation();
    }

    private PrototypeRouteOperationView? BuildActiveRouteOperation()
    {
        if (_selectedContractId is null)
        {
            return null;
        }

        var parsed = TryParseRouteContractId(_selectedContractId);
        if (parsed is null)
        {
            return null;
        }

        var route = Routes.FirstOrDefault(route => route.Id == parsed.Value.RouteId);
        if (route is null || !IsKnownRouteResource(parsed.Value.ResourceId))
        {
            return null;
        }

        var charter = _cities[0];
        if (route.FromNode != charter.Id && route.ToNode != charter.Id)
        {
            return null;
        }

        var sourceId = route.FromNode == charter.Id ? route.ToNode : route.FromNode;
        var source = _cities.FirstOrDefault(city => city.Id == sourceId);
        return source is null
            ? null
            : BuildRouteOperation(route, source, charter, parsed.Value.ResourceId, isActive: true);
    }

    private PrototypeRouteOperationView? BuildRouteOperationFromContract(PrototypeRouteContractView contract, bool isActive)
    {
        var route = Routes.FirstOrDefault(route => route.Id == contract.RouteId);
        var source = _cities.FirstOrDefault(city => city.Id == contract.FromNode);
        var destination = _cities.FirstOrDefault(city => city.Id == contract.ToNode);
        return route is null || source is null || destination is null
            ? null
            : BuildRouteOperation(route, source, destination, contract.ResourceId, isActive);
    }

    private PrototypeRouteOperationView BuildRouteOperation(
        TradeRoute route,
        RuntimeCity source,
        RuntimeCity destination,
        string resourceId,
        bool isActive)
    {
        var allowed = RouteAllowsResource(route.Id, resourceId);
        var exportable = ExportableWarehouseUnits(source, resourceId);
        var demandGap = DemandGap(destination, resourceId);
        var dispatchCap = Math.Min(route.CapacityPerDay, ContractUnits(route));
        var candidateUnits = Math.Min(dispatchCap, Math.Min(exportable, demandGap));
        var price = PriceFor(destination, resourceId);
        var expectedRevenue = RoundMoney(price * candidateUnits);
        var transportCost = RoundMoney(route.CostPerUnit * candidateUnits);
        var expectedNet = RoundMoney(expectedRevenue - transportCost);
        var pausedReason = RouteOperationPausedReason(allowed, exportable, demandGap, candidateUnits, expectedNet);
        var canDispatch = pausedReason.Length == 0;
        var usedCapacity = canDispatch ? candidateUnits : 0;
        var sourceContractId = RouteContractId(route.Id, resourceId);

        return new PrototypeRouteOperationView(
            RouteOperationId(route.Id, source.Id, destination.Id, resourceId),
            sourceContractId,
            route.Id,
            source.Id,
            destination.Id,
            resourceId,
            isActive,
            canDispatch,
            route.CapacityPerDay,
            candidateUnits,
            usedCapacity,
            Math.Max(0, route.CapacityPerDay - usedCapacity),
            ShipmentPriority(destination, resourceId) + RoutePriorityBoost(route.Id, resourceId),
            expectedRevenue,
            transportCost,
            expectedNet,
            canDispatch ? Math.Min(candidateUnits, demandGap) : 0,
            canDispatch ? "ready" : "paused",
            pausedReason,
            ContractPolicyAction(route.Id, destination, resourceId));
    }

    private IReadOnlyList<PrototypeMarketSignal> BuildMarketSignals(RuntimeCity city)
    {
        var needsByResource = _needs.ToDictionary(need => need.ResourceId, StringComparer.Ordinal);
        var prices = _economy
            .CalculatePrices(_content.Resources, city.Market, _needs)
            .ToDictionary(price => price.ResourceId, price => price, StringComparer.Ordinal);
        var signals = new List<PrototypeMarketSignal>();

        foreach (var resource in _content.Resources.OrderBy(resource => resource.Id, StringComparer.Ordinal))
        {
            needsByResource.TryGetValue(resource.Id, out var need);
            var marketStock = city.Market.Get(resource.Id);
            var warehouseStock = city.CompanyWarehouse.Get(resource.Id);
            if (need is null && marketStock == 0 && warehouseStock == 0)
            {
                continue;
            }

            prices.TryGetValue(resource.Id, out var price);
            var desiredStock = Math.Max(0, need?.DesiredStock ?? 0);
            var consumption = Math.Max(0, need?.ConsumptionPerTick ?? 0);
            var policy = EffectivePolicyFor(city, resource.Id, need);
            var shipmentPriority = ShipmentPriority(marketStock, desiredStock, policy.SafetyStock, policy.ReorderPoint);
            signals.Add(new PrototypeMarketSignal(
                resource.Id,
                price?.Price ?? resource.BasePrice,
                price?.Scarcity ?? 0,
                marketStock,
                warehouseStock,
                desiredStock,
                consumption,
                policy.SafetyStock,
                policy.ReorderPoint,
                policy.IsOverride,
                policy.Mode,
                shipmentPriority,
                MarketReason(marketStock, warehouseStock, desiredStock, consumption, price?.Scarcity ?? 0, policy.SafetyStock, policy.ReorderPoint),
                PolicyAction(shipmentPriority, desiredStock, marketStock, consumption, policy.Mode)));
        }

        return signals
            .OrderByDescending(signal => signal.ShipmentPriority)
            .ThenByDescending(signal => signal.Scarcity)
            .ThenBy(signal => signal.ResourceId, StringComparer.Ordinal)
            .ToArray();
    }

    private IReadOnlyList<PrototypeProductionChainOpportunityView> BuildProductionChainOpportunities()
    {
        var opportunities = new List<PrototypeProductionChainOpportunityView>();

        foreach (var city in _cities.OrderBy(city => city.Id, StringComparer.Ordinal))
        {
            var localPrices = _economy
                .CalculatePrices(_content.Resources, city.Market, _needs)
                .ToDictionary(price => price.ResourceId, price => price, StringComparer.Ordinal);

            foreach (var recipe in _content.Recipes.OrderBy(recipe => recipe.Id, StringComparer.Ordinal))
            {
                var inputs = recipe.Inputs
                    .OrderBy(input => input.ResourceId, StringComparer.Ordinal)
                    .Select(input => BuildProductionInputLine(city, input, localPrices))
                    .ToArray();
                var outputs = recipe.Outputs
                    .OrderBy(output => output.ResourceId, StringComparer.Ordinal)
                    .Select(output => BuildProductionOutputLine(city, output, localPrices))
                    .ToArray();
                var missingUnits = inputs.Sum(input => input.MissingAmount);
                var maxRuns = MaxProductionRuns(city, recipe, inputs);
                var isReady = maxRuns > 0;
                var bottleneck = ProductionBottleneck(recipe, inputs, isReady);
                var inputCost = RoundMoney(inputs.Sum(input => input.RequiredAmount * input.LocalUnitPrice));
                var outputValue = RoundMoney(outputs.Sum(output => output.OutputAmount * (output.BestDestinationUnitPrice ?? output.LocalUnitPrice)));
                var margin = RoundMoney(outputValue - inputCost);
                var priority = outputs.Select(output => output.DestinationShipmentPriority).DefaultIfEmpty(0).Max();
                var candidateRouteId = outputs
                    .Where(output => output.BestRouteId is not null)
                    .OrderByDescending(output => output.DestinationShipmentPriority)
                    .ThenByDescending(output => output.BestDestinationUnitPrice ?? output.LocalUnitPrice)
                    .ThenBy(output => output.BestRouteId, StringComparer.Ordinal)
                    .Select(output => output.BestRouteId)
                    .FirstOrDefault();
                var roleBonus = ProductionRoleBonus(city, recipe);
                var score = ProductionScore(isReady, priority, margin, inputs, roleBonus);

                opportunities.Add(new PrototypeProductionChainOpportunityView(
                    $"{city.Id}:{recipe.Id}",
                    city.Id,
                    city.Name,
                    recipe.Id,
                    recipe.BuildingType,
                    ReadOnlyCopy(inputs),
                    ReadOnlyCopy(outputs),
                    maxRuns,
                    inputCost,
                    outputValue,
                    margin,
                    bottleneck,
                    missingUnits,
                    priority,
                    score,
                    isReady,
                    candidateRouteId,
                    ProductionReason(recipe, inputs, outputs, isReady, margin, bottleneck)));
            }
        }

        return ReadOnlyCopy(opportunities
            .OrderByDescending(opportunity => opportunity.IsReady)
            .ThenByDescending(opportunity => opportunity.DestinationShipmentPriority)
            .ThenByDescending(opportunity => opportunity.ExpectedMargin)
            .ThenBy(opportunity => opportunity.MissingInputUnits)
            .ThenByDescending(opportunity => opportunity.Score)
            .ThenBy(opportunity => opportunity.CityId, StringComparer.Ordinal)
            .ThenBy(opportunity => opportunity.RecipeId, StringComparer.Ordinal)
            .ToArray());
    }

    private IReadOnlyList<PrototypeNpcPressureView> BuildNpcPressures(
        IReadOnlyList<PrototypeProductionChainOpportunityView> productionChains,
        IReadOnlyList<PrototypeRouteOperationView> routeOperations,
        PrototypeRouteOperationView? activeOperation)
    {
        var candidates = new List<NpcPressureCandidate>();
        var contexts = new Dictionary<string, NpcPressureContext>(StringComparer.Ordinal);
        var operationInputs = activeOperation is null
            ? routeOperations
            : routeOperations.Concat([activeOperation]).ToArray();

        foreach (var operation in operationInputs
            .GroupBy(operation => operation.Id, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(operation => operation.Id, StringComparer.Ordinal))
        {
            var destination = _cities.FirstOrDefault(city => city.Id == operation.ToNode);
            if (destination is null)
            {
                continue;
            }

            var id = $"npc:{NpcCompanyId}:route:{operation.Id}";
            var strategicBonus = operation.IsActive ? 8m : 3m;
            var reason = $"{operation.Status}; net {SignedMoney(operation.ExpectedNet)}, unmet {operation.UnmetDemandServed}, {operation.PolicyAction}";
            candidates.Add(new NpcPressureCandidate(
                id,
                "contest_route",
                destination.Id,
                operation.RouteId,
                operation.Id,
                null,
                operation.ResourceId,
                operation.ExpectedNet,
                operation.ShipmentPriority,
                operation.UnmetDemandServed,
                operation.CanDispatch,
                strategicBonus,
                reason));
            contexts[id] = new NpcPressureContext(
                destination.Id,
                destination.Name,
                null,
                null,
                operation.RouteId,
                operation.Id,
                null,
                operation.ResourceId,
                operation.CanDispatch);
        }

        foreach (var chain in productionChains.OrderBy(chain => chain.Id, StringComparer.Ordinal))
        {
            var output = chain.Outputs
                .OrderByDescending(line => line.DestinationShipmentPriority)
                .ThenByDescending(line => line.BestDestinationUnitPrice ?? line.LocalUnitPrice)
                .ThenBy(line => line.ResourceId, StringComparer.Ordinal)
                .FirstOrDefault();
            if (output is null)
            {
                continue;
            }

            var targetCityId = output.BestDestinationCityId ?? chain.CityId;
            var targetCity = _cities.FirstOrDefault(city => city.Id == targetCityId);
            var targetCityName = targetCity?.Name ?? chain.CityName;
            var id = $"npc:{NpcCompanyId}:production:{chain.Id}:{output.ResourceId}";
            var intent = chain.IsReady ? "back_production" : "secure_inputs";
            var strategicBonus = (chain.IsReady ? 5m : 1m) + (chain.CandidateRouteId is null ? 0m : 3m);
            var destinationReason = string.Equals(targetCityId, chain.CityId, StringComparison.Ordinal)
                ? "local output"
                : $"demand at {targetCityName}";
            var reason = $"{chain.Reason}; {destinationReason}; margin {SignedMoney(chain.ExpectedMargin)}";

            candidates.Add(new NpcPressureCandidate(
                id,
                intent,
                chain.CityId,
                chain.CandidateRouteId,
                null,
                chain.Id,
                output.ResourceId,
                chain.ExpectedMargin,
                chain.DestinationShipmentPriority,
                Math.Max(0, output.OutputAmount),
                chain.IsReady,
                strategicBonus,
                reason));
            contexts[id] = new NpcPressureContext(
                chain.CityId,
                chain.CityName,
                string.Equals(targetCityId, chain.CityId, StringComparison.Ordinal) ? null : targetCityId,
                string.Equals(targetCityId, chain.CityId, StringComparison.Ordinal) ? null : targetCityName,
                chain.CandidateRouteId,
                null,
                chain.Id,
                output.ResourceId,
                chain.IsReady);
        }

        return ReadOnlyCopy(new DeterministicNpcPressureAi()
            .Rank(NpcCompanyId, candidates)
            .Where(score => score.Pressure > 0m && contexts.ContainsKey(score.CandidateId))
            .Select(score =>
            {
                var context = contexts[score.CandidateId];
                return new PrototypeNpcPressureView(
                    score.CandidateId,
                    score.CompanyId,
                    NpcCompanyName,
                    score.Intent,
                    context.CityId,
                    context.CityName,
                    context.TargetCityId,
                    context.TargetCityName,
                    context.RouteId,
                    context.RouteOperationId,
                    context.ProductionOpportunityId,
                    context.ResourceId,
                    score.Pressure,
                    score.ShipmentPriority,
                    score.ExpectedValue,
                    context.CanContest,
                    score.Reason);
            })
            .ToArray());
    }

    private PrototypeProductionResourceLineView BuildProductionInputLine(
        RuntimeCity city,
        ResourceAmount input,
        IReadOnlyDictionary<string, MarketPrice> localPrices)
    {
        localPrices.TryGetValue(input.ResourceId, out var price);
        var warehouseStock = city.CompanyWarehouse.Get(input.ResourceId);
        var protectedStock = WarehouseReserveFor(city, input.ResourceId);
        var available = Math.Max(0, warehouseStock - protectedStock);
        return new PrototypeProductionResourceLineView(
            input.ResourceId,
            input.Amount,
            0,
            warehouseStock,
            city.Market.Get(input.ResourceId),
            protectedStock,
            available,
            Math.Max(0, input.Amount - available),
            price?.Price ?? _content.Resource(input.ResourceId).BasePrice,
            price?.Scarcity ?? 0,
            null,
            null,
            null,
            0);
    }

    private PrototypeProductionResourceLineView BuildProductionOutputLine(
        RuntimeCity city,
        ResourceAmount output,
        IReadOnlyDictionary<string, MarketPrice> localPrices)
    {
        localPrices.TryGetValue(output.ResourceId, out var price);
        var destination = BestProductionDestination(city, output.ResourceId);
        return new PrototypeProductionResourceLineView(
            output.ResourceId,
            0,
            output.Amount,
            city.CompanyWarehouse.Get(output.ResourceId),
            city.Market.Get(output.ResourceId),
            0,
            city.CompanyWarehouse.Get(output.ResourceId),
            0,
            price?.Price ?? _content.Resource(output.ResourceId).BasePrice,
            price?.Scarcity ?? 0,
            destination?.CityId,
            destination?.RouteId,
            destination?.UnitPrice,
            destination?.ShipmentPriority ?? 0);
    }

    private ProductionDestination? BestProductionDestination(RuntimeCity source, string resourceId)
    {
        return Routes
            .Where(route => (route.FromNode == source.Id || route.ToNode == source.Id) && RouteAllowsResource(route.Id, resourceId))
            .Select(route =>
            {
                var destinationId = route.FromNode == source.Id ? route.ToNode : route.FromNode;
                var destination = _cities.FirstOrDefault(city => city.Id == destinationId);
                if (destination is null)
                {
                    return null;
                }

                var prices = PricesFor(destination);
                var unitPrice = prices.TryGetValue(resourceId, out var price) ? price : _content.Resource(resourceId).BasePrice;
                var priority = ShipmentPriority(destination, resourceId) + RoutePriorityBoost(route.Id, resourceId);
                return new ProductionDestination(destination.Id, route.Id, unitPrice, priority);
            })
            .Where(destination => destination is not null)
            .OrderByDescending(destination => destination!.ShipmentPriority)
            .ThenByDescending(destination => destination!.UnitPrice)
            .ThenBy(destination => destination!.RouteId, StringComparer.Ordinal)
            .ThenBy(destination => destination!.CityId, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private static int MaxProductionRuns(
        RuntimeCity city,
        RecipeDef recipe,
        IReadOnlyList<PrototypeProductionResourceLineView> inputs)
    {
        if (recipe.Inputs.Count == 0)
        {
            return SourceRecipeAvailable(city, recipe) ? 1 : 0;
        }

        if (inputs.Count == 0 || inputs.Any(input => input.RequiredAmount <= 0))
        {
            return 0;
        }

        return inputs.Min(input => input.AvailableAmount / input.RequiredAmount);
    }

    private static bool SourceRecipeAvailable(RuntimeCity city, RecipeDef recipe)
    {
        return recipe.Outputs.Any(output =>
            city.CompanyWarehouse.Get(output.ResourceId) > 0
            || city.Market.Get(output.ResourceId) > 0
            || city.Specialization.AnchorResources.Contains(output.ResourceId, StringComparer.Ordinal)
            || city.Specialization.OutputResources.Contains(output.ResourceId, StringComparer.Ordinal));
    }

    private static string? ProductionBottleneck(
        RecipeDef recipe,
        IReadOnlyList<PrototypeProductionResourceLineView> inputs,
        bool isReady)
    {
        if (isReady)
        {
            return null;
        }

        var missing = inputs
            .Where(input => input.MissingAmount > 0)
            .OrderBy(input => input.ResourceId, StringComparer.Ordinal)
            .FirstOrDefault();
        if (missing is not null)
        {
            return missing.ResourceId;
        }

        return recipe.Outputs
            .OrderBy(output => output.ResourceId, StringComparer.Ordinal)
            .Select(output => output.ResourceId)
            .FirstOrDefault();
    }

    private static decimal ProductionRoleBonus(RuntimeCity city, RecipeDef recipe)
    {
        var favoredOutputs = recipe.Outputs.Any(output =>
            city.Specialization.OutputResources.Contains(output.ResourceId, StringComparer.Ordinal));
        if (favoredOutputs)
        {
            return 8m;
        }

        var anchoredInputs = recipe.Inputs.Any(input =>
            city.Specialization.AnchorResources.Contains(input.ResourceId, StringComparer.Ordinal));
        return anchoredInputs ? 3m : 0m;
    }

    private static decimal ProductionScore(
        bool isReady,
        int destinationPriority,
        decimal margin,
        IReadOnlyList<PrototypeProductionResourceLineView> inputs,
        decimal roleBonus)
    {
        var required = inputs.Sum(input => input.RequiredAmount);
        var missing = inputs.Sum(input => input.MissingAmount);
        var completeness = required <= 0 ? 1m : 1m - missing / (decimal)Math.Max(1, required);
        return RoundMoney(
            (isReady ? 100m : 0m)
            + destinationPriority * 3m
            + margin
            + completeness * 20m
            + roleBonus);
    }

    private static string ProductionReason(
        RecipeDef recipe,
        IReadOnlyList<PrototypeProductionResourceLineView> inputs,
        IReadOnlyList<PrototypeProductionResourceLineView> outputs,
        bool isReady,
        decimal margin,
        string? bottleneck)
    {
        var output = outputs
            .OrderByDescending(line => line.DestinationShipmentPriority)
            .ThenBy(line => line.ResourceId, StringComparer.Ordinal)
            .FirstOrDefault();
        var outputId = output?.ResourceId ?? recipe.Id;

        if (isReady)
        {
            var prefix = recipe.Inputs.Count == 0 ? "source production" : "ready";
            var route = output?.BestRouteId is null ? "local sale" : $"via {output.BestRouteId}";
            return $"{prefix}: {outputId} margin {SignedMoney(margin)}; {route}";
        }

        var protectedInput = inputs
            .Where(input => input.MissingAmount > 0 && input.ProtectedStock > 0 && input.WarehouseStock >= input.RequiredAmount)
            .OrderBy(input => input.ResourceId, StringComparer.Ordinal)
            .FirstOrDefault();
        if (protectedInput is not null)
        {
            return $"{protectedInput.ResourceId} protected by safety stock";
        }

        var missing = inputs
            .Where(input => input.MissingAmount > 0)
            .OrderBy(input => input.ResourceId, StringComparer.Ordinal)
            .FirstOrDefault();
        if (missing is not null)
        {
            return $"missing {missing.ResourceId} {missing.MissingAmount}";
        }

        return bottleneck is null
            ? "output surplus; route export needed"
            : $"source {bottleneck} not local";
    }

    private ProductionReservation? ReservationFor(PrototypeRouteOperationView? operation)
    {
        if (operation is null || string.Equals(operation.PausedReason, "blocked cargo", StringComparison.Ordinal))
        {
            return null;
        }

        var route = Routes.FirstOrDefault(route => route.Id == operation.RouteId);
        var source = _cities.FirstOrDefault(city => city.Id == operation.FromNode);
        if (route is null || source is null)
        {
            return null;
        }

        var dispatchCap = Math.Min(route.CapacityPerDay, ContractUnits(route));
        var amount = Math.Min(ExportableWarehouseUnits(source, operation.ResourceId), dispatchCap);
        return amount <= 0 ? null : new ProductionReservation(source.Id, operation.ResourceId, amount);
    }

    private int DemandGap(RuntimeCity city, string resourceId)
    {
        var desiredStock = Math.Max(0, NeedFor(resourceId)?.DesiredStock ?? 0);
        return Math.Max(0, desiredStock - city.Market.Get(resourceId));
    }

    private static string RouteOperationPausedReason(bool allowed, int exportable, int demandGap, int units, decimal expectedNet)
    {
        if (!allowed)
        {
            return "blocked cargo";
        }

        if (exportable <= 0)
        {
            return "no exportable stock";
        }

        if (demandGap <= 0)
        {
            return "destination stocked";
        }

        if (units <= 0)
        {
            return "no route capacity";
        }

        return expectedNet < 0m ? "negative expected net" : "";
    }

    private static (string RouteId, string ResourceId)? TryParseRouteContractId(string contractId)
    {
        var separator = contractId.IndexOf(':');
        if (separator <= 0 || separator >= contractId.Length - 1)
        {
            return null;
        }

        return (contractId[..separator], contractId[(separator + 1)..]);
    }

    private static string RouteContractId(string routeId, string resourceId)
    {
        return $"{routeId}:{resourceId}";
    }

    private static string RouteOperationId(string routeId, string fromNode, string toNode, string resourceId)
    {
        return $"{routeId}:{fromNode}->{toNode}:{resourceId}";
    }

    private SaveGame BuildSave(IReadOnlyList<MarketPrice> prices, string? selectedContractId)
    {
        var priceState = prices.ToDictionary(price => price.ResourceId, price => price.Price, StringComparer.Ordinal);
        var routePolicyResources = _needs
            .Select(need => need.ResourceId)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var cities = _cities.Select(city => new CitySaveState(
            city.Id,
            city.Level.ToString(),
            new Dictionary<string, int>
            {
                ["peasants"] = city.Population.Peasants,
                ["artisans"] = city.Population.Artisans,
                ["merchants"] = city.Population.Merchants,
                ["elite"] = city.Population.Elite
            },
            city.Districts,
            city.Market.ToDictionary(),
            city.CompanyWarehouse.ToDictionary(),
            priceState)).ToArray();

        return new SaveGame(
            SaveCodec.CurrentSaveVersion,
            _content.ContentHash,
            World.WorldGenVersion,
            World.Seed,
            new RngStreams((ulong)World.Seed, (ulong)World.Seed + 101, (ulong)World.Seed + 202),
            _calendar,
            _company,
            cities,
            Routes.Select(route => new RouteSaveState(route.Id, route.FromNode, route.ToNode, route.Mode, route.CapacityPerDay, routePolicyResources)).ToArray(),
            [],
            new FogOfWarState(_cities.Take(4).Select(city => city.Id).ToArray()),
            _warehousePolicyOverrides
                .OrderBy(kvp => kvp.Key.CityId, StringComparer.Ordinal)
                .ThenBy(kvp => kvp.Key.ResourceId, StringComparer.Ordinal)
                .Select(kvp => new WarehousePolicySaveState(
                    kvp.Key.CityId,
                    kvp.Key.ResourceId,
                    kvp.Value.SafetyStock,
                    kvp.Value.ReorderPoint,
                    ModeForSave(kvp.Value.Mode)))
                .ToArray(),
            BuildRouteSavePolicies(),
            selectedContractId);
    }

    private IReadOnlyList<PrototypeRoutePolicyView> BuildRoutePolicyViews()
    {
        return _routePolicies.Values
            .OrderBy(policy => policy.RouteId, StringComparer.Ordinal)
            .Select(policy => new PrototypeRoutePolicyView(
                policy.RouteId,
                policy.ReservedResources.Order(StringComparer.Ordinal).ToArray(),
                policy.PriorityResourceId))
            .ToArray();
    }

    private IReadOnlyList<RoutePolicySaveState> BuildRouteSavePolicies()
    {
        return _routePolicies.Values
            .OrderBy(policy => policy.RouteId, StringComparer.Ordinal)
            .Select(policy => new RoutePolicySaveState(
                policy.RouteId,
                policy.ReservedResources.Order(StringComparer.Ordinal).ToArray(),
                policy.PriorityResourceId))
            .ToArray();
    }

    private Dictionary<string, decimal> PricesFor(RuntimeCity city)
    {
        return _economy
            .CalculatePrices(_content.Resources, city.Market, _needs)
            .ToDictionary(price => price.ResourceId, price => price.Price, StringComparer.Ordinal);
    }

    private decimal PriceFor(RuntimeCity city, string resourceId)
    {
        return PricesFor(city).TryGetValue(resourceId, out var price) ? price : 1m;
    }

    private double NeedScarcity(RuntimeCity city, MarketNeed need)
    {
        return Math.Clamp((need.DesiredStock - city.Market.Get(need.ResourceId)) / (double)Math.Max(1, need.DesiredStock), 0, 1);
    }

    private int ShipmentPriority(RuntimeCity city, string resourceId)
    {
        var need = NeedFor(resourceId);
        var policy = EffectivePolicyFor(city, resourceId, need);
        return ShipmentPriority(
            city.Market.Get(resourceId),
            Math.Max(0, need?.DesiredStock ?? 0),
            policy.SafetyStock,
            policy.ReorderPoint);
    }

    private string PolicyAction(RuntimeCity city, string resourceId)
    {
        var need = NeedFor(resourceId);
        var policy = EffectivePolicyFor(city, resourceId, need);
        return PolicyAction(
            ShipmentPriority(city, resourceId),
            Math.Max(0, need?.DesiredStock ?? 0),
            city.Market.Get(resourceId),
            Math.Max(0, need?.ConsumptionPerTick ?? 0),
            policy.Mode);
    }

    private string ContractPolicyAction(string routeId, RuntimeCity city, string resourceId)
    {
        var action = PolicyAction(city, resourceId);
        if (!_routePolicies.TryGetValue(routeId, out var policy))
        {
            return action;
        }

        if (string.Equals(policy.PriorityResourceId, resourceId, StringComparison.Ordinal))
        {
            return $"{action}; route priority";
        }

        return policy.ReservedResources.Contains(resourceId, StringComparer.Ordinal)
            ? $"{action}; route reserved"
            : action;
    }

    private int ExportableWarehouseUnits(RuntimeCity city, string resourceId)
    {
        return Math.Max(0, city.CompanyWarehouse.Get(resourceId) - WarehouseReserveFor(city, resourceId));
    }

    private int WarehouseReserveFor(RuntimeCity city, string resourceId)
    {
        var need = NeedFor(resourceId);
        if (need is null)
        {
            return 0;
        }

        return Math.Min(city.CompanyWarehouse.Get(resourceId), EffectivePolicyFor(city, resourceId, need).SafetyStock);
    }

    private MarketNeed? NeedFor(string resourceId)
    {
        return _needs.FirstOrDefault(need => string.Equals(need.ResourceId, resourceId, StringComparison.Ordinal));
    }

    private bool IsKnownPolicyTarget(string cityId, string resourceId)
    {
        return !string.IsNullOrWhiteSpace(cityId)
            && !string.IsNullOrWhiteSpace(resourceId)
            && _cities.Any(city => string.Equals(city.Id, cityId, StringComparison.Ordinal))
            && _needs.Any(need => string.Equals(need.ResourceId, resourceId, StringComparison.Ordinal));
    }

    private bool TryGetRoutePolicy(string routeId, out RoutePolicy policy)
    {
        if (string.IsNullOrWhiteSpace(routeId) || !_routePolicies.TryGetValue(routeId, out var existingPolicy))
        {
            policy = default!;
            return false;
        }

        policy = existingPolicy;
        return true;
    }

    private bool IsKnownRouteResource(string resourceId)
    {
        return !string.IsNullOrWhiteSpace(resourceId)
            && _needs.Any(need => string.Equals(need.ResourceId, resourceId, StringComparison.Ordinal));
    }

    private bool RouteAllowsResource(string routeId, string resourceId)
    {
        return !_routePolicies.TryGetValue(routeId, out var policy)
            || policy.ReservedResources.Contains(resourceId, StringComparer.Ordinal);
    }

    private int RoutePriorityBoost(string routeId, string resourceId)
    {
        return _routePolicies.TryGetValue(routeId, out var policy)
            && string.Equals(policy.PriorityResourceId, resourceId, StringComparison.Ordinal)
            ? 5
            : 0;
    }

    private WarehousePolicy EffectivePolicyFor(RuntimeCity city, string resourceId, MarketNeed? need)
    {
        if (_warehousePolicyOverrides.TryGetValue(new WarehousePolicyKey(city.Id, resourceId), out var policy))
        {
            return new WarehousePolicy(policy.SafetyStock, policy.ReorderPoint, true, policy.Mode);
        }

        return PolicyForMode(need, BalancedWarehouseMode, false);
    }

    private static WarehousePolicy PolicyForMode(MarketNeed? need, string mode, bool isOverride)
    {
        return string.Equals(mode, ConservativeWarehouseMode, StringComparison.Ordinal)
            ? new WarehousePolicy(ConservativeSafetyStockFor(need), ConservativeReorderPointFor(need), isOverride, ConservativeWarehouseMode)
            : new WarehousePolicy(SafetyStockFor(need), ReorderPointFor(need), isOverride, BalancedWarehouseMode);
    }

    private static bool TryNormalizeWarehouseMode(string? mode, out string normalizedMode)
    {
        if (string.IsNullOrWhiteSpace(mode))
        {
            normalizedMode = BalancedWarehouseMode;
            return true;
        }

        var trimmed = mode.Trim();
        if (string.Equals(trimmed, BalancedWarehouseMode, StringComparison.OrdinalIgnoreCase))
        {
            normalizedMode = BalancedWarehouseMode;
            return true;
        }

        if (string.Equals(trimmed, ConservativeWarehouseMode, StringComparison.OrdinalIgnoreCase))
        {
            normalizedMode = ConservativeWarehouseMode;
            return true;
        }

        normalizedMode = BalancedWarehouseMode;
        return false;
    }

    private static string? ModeForSave(string mode)
    {
        return string.Equals(mode, BalancedWarehouseMode, StringComparison.Ordinal)
            ? null
            : mode;
    }

    private static int ContractUnits(TradeRoute route)
    {
        return Math.Max(1, Math.Min(3, route.CapacityPerDay / 4));
    }

    private static int SafetyStockFor(MarketNeed? need)
    {
        if (need is null)
        {
            return 0;
        }

        return Math.Max(need.ConsumptionPerTick * 2, (int)Math.Ceiling(need.DesiredStock * 0.25));
    }

    private static int ReorderPointFor(MarketNeed? need)
    {
        if (need is null)
        {
            return 0;
        }

        return Math.Max(SafetyStockFor(need) + need.ConsumptionPerTick, (int)Math.Ceiling(need.DesiredStock * 0.50));
    }

    private static int ConservativeSafetyStockFor(MarketNeed? need)
    {
        if (need is null)
        {
            return 0;
        }

        return Math.Max(SafetyStockFor(need) + need.ConsumptionPerTick, (int)Math.Ceiling(need.DesiredStock * 0.40));
    }

    private static int ConservativeReorderPointFor(MarketNeed? need)
    {
        if (need is null)
        {
            return 0;
        }

        return Math.Max(ConservativeSafetyStockFor(need) + need.ConsumptionPerTick, (int)Math.Ceiling(need.DesiredStock * 0.70));
    }

    private static int ShipmentPriority(int marketStock, int desiredStock, int safetyStock, int reorderPoint)
    {
        if (desiredStock <= 0)
        {
            return 0;
        }

        if (marketStock <= 0)
        {
            return 4;
        }

        if (marketStock < safetyStock)
        {
            return 3;
        }

        if (marketStock < reorderPoint)
        {
            return 2;
        }

        return marketStock < desiredStock ? 1 : 0;
    }

    public static int ConsumeNeeds(Inventory market, IEnumerable<MarketNeed> needs)
    {
        var consumed = 0;
        foreach (var need in needs.OrderBy(need => need.ResourceId, StringComparer.Ordinal))
        {
            var amount = Math.Min(market.Get(need.ResourceId), Math.Max(0, need.ConsumptionPerTick));
            if (amount > 0 && market.TryRemove(need.ResourceId, amount))
            {
                consumed += amount;
            }
        }

        return consumed;
    }

    private bool CanRunInCity(RuntimeCity city, RecipeDef recipe, ProductionReservation? reservation)
    {
        if (recipe.Inputs.Count == 0)
        {
            return recipe.Outputs.Any(output => city.CompanyWarehouse.Get(output.ResourceId) > 0 || city.Market.Get(output.ResourceId) > 0);
        }

        return recipe.Inputs.All(input => AvailableForProduction(city, input.ResourceId, reservation) >= input.Amount);
    }

    private int AvailableForProduction(RuntimeCity city, string resourceId, ProductionReservation? reservation)
    {
        var stock = ExportableWarehouseUnits(city, resourceId);
        if (reservation is not null && reservation.CityId == city.Id && reservation.ResourceId == resourceId)
        {
            stock = Math.Max(0, stock - reservation.Amount);
        }

        return stock;
    }

    private static PopulationCohorts ApplyPopulationDelta(PopulationCohorts population, int delta)
    {
        return population with { Peasants = Math.Max(0, population.Peasants + delta) };
    }

    private static string NameFor(WorldNode node, int index)
    {
        var prefix = node.Kind switch
        {
            "charter_town" => "Charter",
            "port" => "Port",
            _ => "Market"
        };
        return $"{prefix} {index + 1}";
    }

    private static PrototypeCitySpecialization SpecializationFor(WorldNode node)
    {
        var resources = node.Resources.Order(StringComparer.Ordinal).ToArray();

        if (string.Equals(node.Kind, "charter_town", StringComparison.Ordinal))
        {
            return new PrototypeCitySpecialization(
                "charter_hub",
                "Charter Hub",
                resources.Take(2).ToArray(),
                [],
                "Coordinates company charters and market flows.");
        }

        if (string.Equals(node.Kind, "port", StringComparison.Ordinal) && resources.Contains("fish", StringComparer.Ordinal))
        {
            return new PrototypeCitySpecialization(
                "fishery_port",
                "Fishery Port",
                ["fish"],
                ["fish"],
                "Turns coastal access into dependable food supply.");
        }

        if (resources.Contains("iron", StringComparer.Ordinal))
        {
            return new PrototypeCitySpecialization(
                "ironworks",
                "Ironworks",
                ["iron"],
                ["tools"],
                "Anchors future toolmaking from local ore.");
        }

        if (resources.Contains("clay", StringComparer.Ordinal))
        {
            return new PrototypeCitySpecialization(
                "kiln_town",
                "Kiln Town",
                ["clay"],
                ["ceramics"],
                "Supplies clay for civic craft production.");
        }

        if (resources.Contains("wool", StringComparer.Ordinal))
        {
            return new PrototypeCitySpecialization(
                "textile_market",
                "Textile Market",
                ["wool"],
                ["cloth"],
                "Feeds cloth production from local pasture.");
        }

        if (resources.Contains("grain", StringComparer.Ordinal))
        {
            return new PrototypeCitySpecialization(
                "grain_market",
                "Grain Market",
                ["grain"],
                ["grain", "bread"],
                "Supports staple supply and breadmaking.");
        }

        if (resources.Contains("wood", StringComparer.Ordinal))
        {
            return new PrototypeCitySpecialization(
                "timber_depot",
                "Timber Depot",
                ["wood"],
                ["wood"],
                "Feeds construction, fuel, and craft inputs.");
        }

        return new PrototypeCitySpecialization(
            "market_exchange",
            "Market Exchange",
            resources,
            [],
            "Balances regional trade around local stock.");
    }

    private static PrototypeCitySpecialization CopySpecialization(PrototypeCitySpecialization specialization)
    {
        return specialization with
        {
            AnchorResources = ReadOnlyCopy(specialization.AnchorResources),
            OutputResources = ReadOnlyCopy(specialization.OutputResources)
        };
    }

    private static IReadOnlyList<T> ReadOnlyCopy<T>(IEnumerable<T> values)
    {
        return Array.AsReadOnly(values.ToArray());
    }

    private static string Signed(int value)
    {
        return value >= 0 ? $"+{value}" : value.ToString();
    }

    private static decimal RoundMoney(decimal value)
    {
        return decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    private static string SignedMoney(decimal value)
    {
        return value.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture);
    }

    private static string NpcIntentLabel(string intent)
    {
        return intent switch
        {
            "contest_route" => "contests route flow for",
            "back_production" => "backs production of",
            "secure_inputs" => "secures inputs for",
            _ => "pressures"
        };
    }

    private static string PolicyAction(int shipmentPriority, int desiredStock, int marketStock, int consumptionPerTick, string mode)
    {
        var action = shipmentPriority switch
        {
            4 => "emergency shipment",
            3 => "protect safety stock",
            2 => "reorder now",
            1 => "top up when capacity allows",
            _ => desiredStock > 0 && consumptionPerTick > 0 && marketStock >= desiredStock + consumptionPerTick * 2
                ? "surplus; export above target"
                : "hold"
        };

        return string.Equals(mode, ConservativeWarehouseMode, StringComparison.Ordinal)
            ? $"{action}; conservative mode"
            : action;
    }

    private static string MarketReason(int marketStock, int warehouseStock, int desiredStock, int consumptionPerTick, double scarcity, int safetyStock, int reorderPoint)
    {
        if (desiredStock <= 0)
        {
            return warehouseStock > 0 ? "export stock only" : "nonessential";
        }

        if (marketStock <= 0)
        {
            return warehouseStock > 0
                ? $"stockout; {warehouseStock} held in company warehouse"
                : "stockout; no local buffer";
        }

        if (consumptionPerTick > 0 && marketStock < consumptionPerTick)
        {
            return $"critical; less than one tick of demand";
        }

        if (marketStock < safetyStock)
        {
            return $"below safety stock {safetyStock}";
        }

        if (marketStock < reorderPoint)
        {
            return $"below reorder point {reorderPoint}";
        }

        if (marketStock < desiredStock)
        {
            return $"short {desiredStock - marketStock} vs target";
        }

        if (scarcity <= -0.35)
        {
            return "surplus; export candidate";
        }

        return "stable";
    }

    private sealed class RuntimeCity(
        string id,
        string name,
        int x,
        int y,
        CityLevel level,
        PopulationCohorts population,
        IReadOnlyList<string> districts,
        PrototypeCitySpecialization specialization,
        Inventory market,
        Inventory companyWarehouse,
        double supplySatisfaction)
    {
        public string Id { get; } = id;
        public string Name { get; } = name;
        public int X { get; } = x;
        public int Y { get; } = y;
        public CityLevel Level { get; set; } = level;
        public PopulationCohorts Population { get; set; } = population;
        public IReadOnlyList<string> Districts { get; } = districts;
        public PrototypeCitySpecialization Specialization { get; } = specialization;
        public Inventory Market { get; } = market;
        public Inventory CompanyWarehouse { get; } = companyWarehouse;
        public double SupplySatisfaction { get; set; } = supplySatisfaction;
    }

    private sealed record ProductionReservation(string CityId, string ResourceId, int Amount);

    private sealed record NpcPressureContext(
        string CityId,
        string CityName,
        string? TargetCityId,
        string? TargetCityName,
        string? RouteId,
        string? RouteOperationId,
        string? ProductionOpportunityId,
        string ResourceId,
        bool CanContest);

    private sealed record WarehousePolicyKey(string CityId, string ResourceId);

    private sealed record WarehousePolicyOverride(int SafetyStock, int ReorderPoint, string Mode);

    private sealed record WarehousePolicy(int SafetyStock, int ReorderPoint, bool IsOverride, string Mode);

    private sealed record RoutePolicy(string RouteId, IReadOnlyList<string> ReservedResources, string? PriorityResourceId);

    private sealed record ProductionDestination(string CityId, string RouteId, decimal UnitPrice, int ShipmentPriority);
}
