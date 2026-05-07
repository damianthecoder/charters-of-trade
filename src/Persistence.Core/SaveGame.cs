using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChartersOfTrade.Persistence.Core;

public sealed record RngStreams(ulong World, ulong Events, ulong Ai);

public sealed record CalendarState(int Year, int DayOfYear);

public sealed record CompanyState(decimal Cash, decimal Debt, int Reputation, string CharterType);

public sealed record CitySaveState(
    string Id,
    string Level,
    IReadOnlyDictionary<string, int> Population,
    IReadOnlyList<string> Districts,
    IReadOnlyDictionary<string, int> MarketStock,
    IReadOnlyDictionary<string, int> CompanyWarehouse,
    IReadOnlyDictionary<string, decimal> PriceState);

public sealed record RouteSaveState(
    string Id,
    string FromNode,
    string ToNode,
    string Mode,
    int CapacityPerDay,
    IReadOnlyList<string> ReservedFor);

public sealed record EventSaveState(string Id, string State, int DaysRemaining);

public sealed record FogOfWarState(IReadOnlyList<string> DiscoveredNodes);

public sealed record WarehousePolicySaveState(
    string CityId,
    string ResourceId,
    int SafetyStock,
    int ReorderPoint,
    string? Mode = null);

public sealed record RoutePolicySaveState(
    string RouteId,
    IReadOnlyList<string> ReservedResources,
    string? PriorityResourceId);

public sealed record ProductionPolicySaveState(
    string CityId,
    string? FocusRecipeId,
    string Mode);

public sealed record RouteOperationSaveState(
    string Id,
    string SourceContractId,
    string RouteId,
    string FromNode,
    string ToNode,
    string ResourceId,
    int UnitsPerDispatch);

public sealed record RouteTransitSaveState(
    string Id,
    string OperationId,
    string RouteId,
    string FromNode,
    string ToNode,
    string ResourceId,
    int Units,
    int DispatchedTick,
    int ArrivalTick,
    decimal ExpectedRevenue,
    decimal TransportCost);

public sealed record ScenarioObjectiveSaveState(
    string ScenarioId,
    int RulesVersion,
    int StartedTick,
    int CurrentTick,
    int? EndTick,
    string EndReason,
    IReadOnlyList<string> CompletedCharterIds,
    IReadOnlyList<string> CompletedCharterResourceIds,
    IReadOnlyDictionary<string, int> StableNeedStreaks,
    decimal FinalCash,
    int FinalScore);

public sealed record SaveGame(
    int SaveVersion,
    string ContentHash,
    string WorldGenVersion,
    int WorldSeed,
    RngStreams SessionRng,
    CalendarState Calendar,
    CompanyState Company,
    IReadOnlyList<CitySaveState> Cities,
    IReadOnlyList<RouteSaveState> Routes,
    IReadOnlyList<EventSaveState> Events,
    FogOfWarState FogOfWar,
    IReadOnlyList<WarehousePolicySaveState> WarehousePolicies,
    IReadOnlyList<RoutePolicySaveState> RoutePolicies,
    IReadOnlyList<ProductionPolicySaveState> ProductionPolicies,
    IReadOnlyList<RouteOperationSaveState> RouteOperations,
    IReadOnlyList<RouteTransitSaveState> RouteTransits,
    string? PendingRouteContractId,
    ScenarioObjectiveSaveState ScenarioObjective);

public static class SaveCodec
{
    public const int CurrentSaveVersion = 5;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize(SaveGame save)
    {
        SaveValidator.Validate(save);
        return JsonSerializer.Serialize(save, Options);
    }

    public static SaveGame Deserialize(string json)
    {
        var save = JsonSerializer.Deserialize<SaveGame>(json, Options)
            ?? throw new InvalidOperationException("Save payload did not contain a save game.");
        SaveValidator.Validate(save);
        return save;
    }

    public static string ComputeStateHash(SaveGame save)
    {
        SaveValidator.Validate(save);
        var canonical = JsonSerializer.Serialize(Normalize(save), Options);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return "sha256:" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static SaveGame Normalize(SaveGame save)
    {
        return save with
        {
            Cities = save.Cities.OrderBy(city => city.Id, StringComparer.Ordinal).ToArray(),
            Routes = save.Routes.OrderBy(route => route.Id, StringComparer.Ordinal).ToArray(),
            Events = save.Events.OrderBy(evt => evt.Id, StringComparer.Ordinal).ToArray(),
            WarehousePolicies = save.WarehousePolicies
                .Select(policy => policy with
                {
                    Mode = NormalizeWarehousePolicyMode(policy.Mode)
                })
                .OrderBy(policy => policy.CityId, StringComparer.Ordinal)
                .ThenBy(policy => policy.ResourceId, StringComparer.Ordinal)
                .ToArray(),
            RoutePolicies = save.RoutePolicies
                .Select(policy => policy with
                {
                    ReservedResources = policy.ReservedResources.Order(StringComparer.Ordinal).ToArray()
                })
                .OrderBy(policy => policy.RouteId, StringComparer.Ordinal)
                .ToArray(),
            ProductionPolicies = save.ProductionPolicies
                .Where(policy => !IsDefaultProductionPolicy(policy))
                .Select(policy => policy with
                {
                    CityId = policy.CityId.Trim(),
                    FocusRecipeId = string.IsNullOrWhiteSpace(policy.FocusRecipeId) ? null : policy.FocusRecipeId.Trim(),
                    Mode = policy.Mode.Trim()
                })
                .OrderBy(policy => policy.CityId, StringComparer.Ordinal)
                .ToArray(),
            RouteOperations = save.RouteOperations
                .OrderBy(operation => operation.Id, StringComparer.Ordinal)
                .ToArray(),
            RouteTransits = save.RouteTransits
                .OrderBy(transit => transit.Id, StringComparer.Ordinal)
                .ToArray(),
            FogOfWar = save.FogOfWar with
            {
                DiscoveredNodes = save.FogOfWar.DiscoveredNodes.Order(StringComparer.Ordinal).ToArray()
            },
            ScenarioObjective = NormalizeScenarioObjective(save.ScenarioObjective)
        };
    }

    private static bool IsDefaultProductionPolicy(ProductionPolicySaveState policy)
    {
        return string.Equals(policy.Mode, "auto", StringComparison.Ordinal) && string.IsNullOrWhiteSpace(policy.FocusRecipeId);
    }

    private static string? NormalizeWarehousePolicyMode(string? mode)
    {
        return string.Equals(mode, "balanced", StringComparison.Ordinal)
            ? null
            : mode;
    }

    private static ScenarioObjectiveSaveState NormalizeScenarioObjective(ScenarioObjectiveSaveState objective)
    {
        return objective with
        {
            ScenarioId = objective.ScenarioId.Trim(),
            EndReason = objective.EndReason.Trim(),
            CompletedCharterIds = objective.CompletedCharterIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray(),
            CompletedCharterResourceIds = objective.CompletedCharterResourceIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray(),
            StableNeedStreaks = objective.StableNeedStreaks
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Key))
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(pair => pair.Key.Trim(), pair => pair.Value, StringComparer.Ordinal)
        };
    }
}

public sealed class SaveValidationException(IReadOnlyList<string> errors)
    : Exception("Save validation failed: " + string.Join("; ", errors))
{
    public IReadOnlyList<string> Errors { get; } = errors;
}

public static class SaveValidator
{
    public static void Validate(SaveGame save)
    {
        var errors = new List<string>();

        if (save.SaveVersion != SaveCodec.CurrentSaveVersion)
        {
            errors.Add($"saveVersion must be {SaveCodec.CurrentSaveVersion}");
        }

        if (string.IsNullOrWhiteSpace(save.ContentHash))
        {
            errors.Add("contentHash must not be empty");
        }

        if (string.IsNullOrWhiteSpace(save.WorldGenVersion))
        {
            errors.Add("worldGenVersion must not be empty");
        }

        if (save.Calendar.Year <= 0)
        {
            errors.Add("calendar year must be positive");
        }

        if (save.Calendar.DayOfYear is < 1 or > 366)
        {
            errors.Add("calendar dayOfYear must be between 1 and 366");
        }

        if (save.Company.Debt < 0)
        {
            errors.Add("company debt must not be negative");
        }

        foreach (var city in save.Cities)
        {
            ValidateCity(city, errors);
        }

        var seenRouteIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var route in save.Routes)
        {
            if (string.IsNullOrWhiteSpace(route.Id))
            {
                errors.Add("route id must not be empty");
            }
            else if (!seenRouteIds.Add(route.Id))
            {
                errors.Add($"route '{route.Id}' must not be duplicated");
            }

            if (route.CapacityPerDay <= 0)
            {
                errors.Add($"route '{route.Id}' capacityPerDay must be positive");
            }

            if (route.ReservedFor is null)
            {
                errors.Add($"route '{route.Id}' reservedFor must not be null");
            }
            else
            {
                var reservedFor = new HashSet<string>(StringComparer.Ordinal);
                foreach (var resourceId in route.ReservedFor)
                {
                    if (string.IsNullOrWhiteSpace(resourceId))
                    {
                        errors.Add($"route '{route.Id}' reservedFor resource id must not be empty");
                        continue;
                    }

                    if (!reservedFor.Add(resourceId))
                    {
                        errors.Add($"route '{route.Id}' reservedFor resource '{resourceId}' must not be duplicated");
                    }
                }
            }
        }

        if (save.WarehousePolicies is null)
        {
            errors.Add("warehousePolicies must not be null");
        }
        else
        {
            var seenPolicies = new HashSet<string>(StringComparer.Ordinal);
            foreach (var policy in save.WarehousePolicies)
            {
                var policyKey = $"{policy.CityId}:{policy.ResourceId}";
                if (!seenPolicies.Add(policyKey))
                {
                    errors.Add($"warehouse policy '{policyKey}' must not be duplicated");
                }

                if (string.IsNullOrWhiteSpace(policy.CityId))
                {
                    errors.Add("warehouse policy cityId must not be empty");
                }

                if (string.IsNullOrWhiteSpace(policy.ResourceId))
                {
                    errors.Add("warehouse policy resourceId must not be empty");
                }

                if (policy.SafetyStock < 0)
                {
                    errors.Add($"warehouse policy '{policy.CityId}:{policy.ResourceId}' safetyStock must not be negative");
                }

                if (policy.ReorderPoint < 0)
                {
                    errors.Add($"warehouse policy '{policy.CityId}:{policy.ResourceId}' reorderPoint must not be negative");
                }

                if (policy.ReorderPoint < policy.SafetyStock)
                {
                    errors.Add($"warehouse policy '{policy.CityId}:{policy.ResourceId}' reorderPoint must not be below safetyStock");
                }

                if (policy.Mode is not null && string.IsNullOrWhiteSpace(policy.Mode))
                {
                    errors.Add($"warehouse policy '{policy.CityId}:{policy.ResourceId}' mode must not be empty when present");
                }
                else if (policy.Mode is not null
                    && !string.Equals(policy.Mode, "balanced", StringComparison.Ordinal)
                    && !string.Equals(policy.Mode, "conservative", StringComparison.Ordinal))
                {
                    errors.Add($"warehouse policy '{policy.CityId}:{policy.ResourceId}' mode must be balanced or conservative");
                }
            }
        }

        if (save.RoutePolicies is null)
        {
            errors.Add("routePolicies must not be null");
        }
        else
        {
            ValidateRoutePolicies(save, errors);
        }

        if (save.ProductionPolicies is null)
        {
            errors.Add("productionPolicies must not be null");
        }
        else
        {
            ValidateProductionPolicies(save, errors);
        }

        if (save.RouteOperations is null)
        {
            errors.Add("routeOperations must not be null");
        }
        else
        {
            ValidateRouteOperations(save, errors);
        }

        if (save.RouteTransits is null)
        {
            errors.Add("routeTransits must not be null");
        }
        else
        {
            ValidateRouteTransits(save, errors);
        }

        if (save.PendingRouteContractId is not null && string.IsNullOrWhiteSpace(save.PendingRouteContractId))
        {
            errors.Add("pendingRouteContractId must not be empty when present");
        }

        ValidateScenarioObjective(save.ScenarioObjective, errors);

        if (errors.Count > 0)
        {
            throw new SaveValidationException(errors);
        }
    }

    private static void ValidateCity(CitySaveState city, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(city.Id))
        {
            errors.Add("city id must not be empty");
        }

        foreach (var (cohort, amount) in city.Population)
        {
            if (amount < 0)
            {
                errors.Add($"city '{city.Id}' population '{cohort}' must not be negative");
            }
        }

        ValidateStock(city.Id, "market", city.MarketStock, errors);
        ValidateStock(city.Id, "warehouse", city.CompanyWarehouse, errors);
    }

    private static void ValidateStock(string cityId, string stockName, IReadOnlyDictionary<string, int> stock, List<string> errors)
    {
        foreach (var (resourceId, amount) in stock)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
            {
                errors.Add($"city '{cityId}' {stockName} resource id must not be empty");
            }

            if (amount < 0)
            {
                errors.Add($"city '{cityId}' {stockName} resource '{resourceId}' must not be negative");
            }
        }
    }

    private static void ValidateRoutePolicies(SaveGame save, List<string> errors)
    {
        var routesById = save.Routes
            .Where(route => !string.IsNullOrWhiteSpace(route.Id))
            .GroupBy(route => route.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var knownRouteIds = routesById.Keys.ToHashSet(StringComparer.Ordinal);
        var seenRoutes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var policy in save.RoutePolicies)
        {
            RouteSaveState? savedRoute = null;
            if (string.IsNullOrWhiteSpace(policy.RouteId))
            {
                errors.Add("route policy routeId must not be empty");
            }
            else if (!routesById.TryGetValue(policy.RouteId, out savedRoute))
            {
                errors.Add($"route policy '{policy.RouteId}' must reference a saved route");
            }

            if (!seenRoutes.Add(policy.RouteId))
            {
                errors.Add($"route policy '{policy.RouteId}' must not be duplicated");
            }

            if (policy.ReservedResources is null)
            {
                errors.Add($"route policy '{policy.RouteId}' reservedResources must not be null");
                continue;
            }

            var reservedResources = new HashSet<string>(StringComparer.Ordinal);
            var validRouteResources = savedRoute?.ReservedFor is null
                ? new HashSet<string>(StringComparer.Ordinal)
                : savedRoute.ReservedFor.ToHashSet(StringComparer.Ordinal);
            foreach (var resourceId in policy.ReservedResources)
            {
                if (string.IsNullOrWhiteSpace(resourceId))
                {
                    errors.Add($"route policy '{policy.RouteId}' reserved resource id must not be empty");
                    continue;
                }

                if (!reservedResources.Add(resourceId))
                {
                    errors.Add($"route policy '{policy.RouteId}' reserved resource '{resourceId}' must not be duplicated");
                }

                if (savedRoute is not null && !validRouteResources.Contains(resourceId))
                {
                    errors.Add($"route policy '{policy.RouteId}' reserved resource '{resourceId}' must be listed in the saved route reservedFor resources");
                }
            }

            if (policy.PriorityResourceId is not null && string.IsNullOrWhiteSpace(policy.PriorityResourceId))
            {
                errors.Add($"route policy '{policy.RouteId}' priorityResourceId must not be empty when present");
            }

            if (policy.PriorityResourceId is not null && !reservedResources.Contains(policy.PriorityResourceId))
            {
                errors.Add($"route policy '{policy.RouteId}' priorityResourceId must be one of reservedResources");
            }
        }

        foreach (var routeId in knownRouteIds.Order(StringComparer.Ordinal))
        {
            if (!seenRoutes.Contains(routeId))
            {
                errors.Add($"route policy '{routeId}' must be present for every saved route");
            }
        }
    }

    private static void ValidateProductionPolicies(SaveGame save, List<string> errors)
    {
        var knownCityIds = save.Cities
            .Where(city => !string.IsNullOrWhiteSpace(city.Id))
            .Select(city => city.Id)
            .ToHashSet(StringComparer.Ordinal);
        var seenCityIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var policy in save.ProductionPolicies)
        {
            if (string.IsNullOrWhiteSpace(policy.CityId))
            {
                errors.Add("production policy cityId must not be empty");
            }
            else
            {
                if (!seenCityIds.Add(policy.CityId))
                {
                    errors.Add($"production policy '{policy.CityId}' must not be duplicated");
                }

                if (!knownCityIds.Contains(policy.CityId))
                {
                    errors.Add($"production policy '{policy.CityId}' must reference a saved city");
                }
            }

            var validMode = string.Equals(policy.Mode, "auto", StringComparison.Ordinal)
                || string.Equals(policy.Mode, "focus", StringComparison.Ordinal)
                || string.Equals(policy.Mode, "paused", StringComparison.Ordinal);
            if (string.IsNullOrWhiteSpace(policy.Mode))
            {
                errors.Add($"production policy '{policy.CityId}' mode must not be empty");
            }
            else if (!validMode)
            {
                errors.Add($"production policy '{policy.CityId}' mode must be auto, focus, or paused");
            }

            if (policy.FocusRecipeId is not null && string.IsNullOrWhiteSpace(policy.FocusRecipeId))
            {
                errors.Add($"production policy '{policy.CityId}' focusRecipeId must not be empty when present");
            }

            if (string.Equals(policy.Mode, "focus", StringComparison.Ordinal) && string.IsNullOrWhiteSpace(policy.FocusRecipeId))
            {
                errors.Add($"production policy '{policy.CityId}' focusRecipeId must be present in focus mode");
            }

            if (!string.Equals(policy.Mode, "focus", StringComparison.Ordinal) && policy.FocusRecipeId is not null)
            {
                errors.Add($"production policy '{policy.CityId}' focusRecipeId must only be present in focus mode");
            }
        }
    }

    private static void ValidateRouteOperations(SaveGame save, List<string> errors)
    {
        var routesById = save.Routes
            .Where(route => !string.IsNullOrWhiteSpace(route.Id))
            .GroupBy(route => route.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        var seenRouteCargo = new HashSet<string>(StringComparer.Ordinal);

        foreach (var operation in save.RouteOperations)
        {
            if (string.IsNullOrWhiteSpace(operation.Id))
            {
                errors.Add("route operation id must not be empty");
            }
            else if (!seenIds.Add(operation.Id))
            {
                errors.Add($"route operation '{operation.Id}' must not be duplicated");
            }

            if (string.IsNullOrWhiteSpace(operation.SourceContractId))
            {
                errors.Add($"route operation '{operation.Id}' sourceContractId must not be empty");
            }

            RouteSaveState? route = null;
            if (string.IsNullOrWhiteSpace(operation.RouteId))
            {
                errors.Add($"route operation '{operation.Id}' routeId must not be empty");
            }
            else if (!routesById.TryGetValue(operation.RouteId, out route))
            {
                errors.Add($"route operation '{operation.Id}' must reference a saved route");
            }

            if (string.IsNullOrWhiteSpace(operation.FromNode))
            {
                errors.Add($"route operation '{operation.Id}' fromNode must not be empty");
            }

            if (string.IsNullOrWhiteSpace(operation.ToNode))
            {
                errors.Add($"route operation '{operation.Id}' toNode must not be empty");
            }

            if (string.IsNullOrWhiteSpace(operation.ResourceId))
            {
                errors.Add($"route operation '{operation.Id}' resourceId must not be empty");
            }

            if (operation.UnitsPerDispatch <= 0)
            {
                errors.Add($"route operation '{operation.Id}' unitsPerDispatch must be positive");
            }

            if (route is null)
            {
                continue;
            }

            var touchesEndpoints =
                (string.Equals(operation.FromNode, route.FromNode, StringComparison.Ordinal) && string.Equals(operation.ToNode, route.ToNode, StringComparison.Ordinal))
                || (string.Equals(operation.FromNode, route.ToNode, StringComparison.Ordinal) && string.Equals(operation.ToNode, route.FromNode, StringComparison.Ordinal));
            if (!touchesEndpoints)
            {
                errors.Add($"route operation '{operation.Id}' fromNode/toNode must be the saved route endpoints");
            }

            if (!route.ReservedFor.Contains(operation.ResourceId, StringComparer.Ordinal))
            {
                errors.Add($"route operation '{operation.Id}' resourceId must be listed in the saved route reservedFor resources");
            }

            var routeCargoKey = $"{operation.RouteId}:{operation.FromNode}>{operation.ToNode}:{operation.ResourceId}";
            if (!seenRouteCargo.Add(routeCargoKey))
            {
                errors.Add($"route operation '{routeCargoKey}' must not be duplicated");
            }
        }
    }

    private static void ValidateRouteTransits(SaveGame save, List<string> errors)
    {
        var routesById = save.Routes
            .Where(route => !string.IsNullOrWhiteSpace(route.Id))
            .GroupBy(route => route.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var operationsById = save.RouteOperations?
            .Where(operation => !string.IsNullOrWhiteSpace(operation.Id))
            .GroupBy(operation => operation.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal)
            ?? new Dictionary<string, RouteOperationSaveState>(StringComparer.Ordinal);
        var seenIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var transit in save.RouteTransits)
        {
            RouteOperationSaveState? operation = null;
            if (string.IsNullOrWhiteSpace(transit.Id))
            {
                errors.Add("route transit id must not be empty");
            }
            else if (!seenIds.Add(transit.Id))
            {
                errors.Add($"route transit '{transit.Id}' must not be duplicated");
            }

            if (string.IsNullOrWhiteSpace(transit.OperationId))
            {
                errors.Add($"route transit '{transit.Id}' operationId must not be empty");
            }
            else if (!operationsById.TryGetValue(transit.OperationId, out operation))
            {
                errors.Add($"route transit '{transit.Id}' must reference a saved route operation");
            }

            RouteSaveState? route = null;
            if (string.IsNullOrWhiteSpace(transit.RouteId))
            {
                errors.Add($"route transit '{transit.Id}' routeId must not be empty");
            }
            else if (!routesById.TryGetValue(transit.RouteId, out route))
            {
                errors.Add($"route transit '{transit.Id}' must reference a saved route");
            }

            if (string.IsNullOrWhiteSpace(transit.FromNode))
            {
                errors.Add($"route transit '{transit.Id}' fromNode must not be empty");
            }

            if (string.IsNullOrWhiteSpace(transit.ToNode))
            {
                errors.Add($"route transit '{transit.Id}' toNode must not be empty");
            }

            if (string.IsNullOrWhiteSpace(transit.ResourceId))
            {
                errors.Add($"route transit '{transit.Id}' resourceId must not be empty");
            }

            if (transit.Units <= 0)
            {
                errors.Add($"route transit '{transit.Id}' units must be positive");
            }

            if (transit.DispatchedTick < 0)
            {
                errors.Add($"route transit '{transit.Id}' dispatchedTick must not be negative");
            }

            if (transit.ArrivalTick <= transit.DispatchedTick)
            {
                errors.Add($"route transit '{transit.Id}' arrivalTick must be after dispatchedTick");
            }

            if (transit.ExpectedRevenue < 0m)
            {
                errors.Add($"route transit '{transit.Id}' expectedRevenue must not be negative");
            }

            if (transit.TransportCost < 0m)
            {
                errors.Add($"route transit '{transit.Id}' transportCost must not be negative");
            }

            if (route is null)
            {
                continue;
            }

            var touchesEndpoints =
                (string.Equals(transit.FromNode, route.FromNode, StringComparison.Ordinal) && string.Equals(transit.ToNode, route.ToNode, StringComparison.Ordinal))
                || (string.Equals(transit.FromNode, route.ToNode, StringComparison.Ordinal) && string.Equals(transit.ToNode, route.FromNode, StringComparison.Ordinal));
            if (!touchesEndpoints)
            {
                errors.Add($"route transit '{transit.Id}' fromNode/toNode must be the saved route endpoints");
            }

            if (operation is not null)
            {
                if (!string.Equals(transit.RouteId, operation.RouteId, StringComparison.Ordinal)
                    || !string.Equals(transit.FromNode, operation.FromNode, StringComparison.Ordinal)
                    || !string.Equals(transit.ToNode, operation.ToNode, StringComparison.Ordinal)
                    || !string.Equals(transit.ResourceId, operation.ResourceId, StringComparison.Ordinal))
                {
                    errors.Add($"route transit '{transit.Id}' must match its saved route operation");
                }
            }
        }
    }

    private static void ValidateScenarioObjective(ScenarioObjectiveSaveState? objective, List<string> errors)
    {
        if (objective is null)
        {
            errors.Add("scenarioObjective must not be null");
            return;
        }

        if (string.IsNullOrWhiteSpace(objective.ScenarioId))
        {
            errors.Add("scenarioObjective scenarioId must not be empty");
        }

        if (objective.RulesVersion <= 0)
        {
            errors.Add("scenarioObjective rulesVersion must be positive");
        }

        if (objective.StartedTick < 0)
        {
            errors.Add("scenarioObjective startedTick must not be negative");
        }

        if (objective.CurrentTick < objective.StartedTick)
        {
            errors.Add("scenarioObjective currentTick must not be before startedTick");
        }

        var validEndReason = string.Equals(objective.EndReason, "in_progress", StringComparison.Ordinal)
            || string.Equals(objective.EndReason, "won", StringComparison.Ordinal)
            || string.Equals(objective.EndReason, "bankrupt", StringComparison.Ordinal)
            || string.Equals(objective.EndReason, "timeout", StringComparison.Ordinal);
        if (!validEndReason)
        {
            errors.Add("scenarioObjective endReason must be in_progress, won, bankrupt, or timeout");
        }

        if (string.Equals(objective.EndReason, "in_progress", StringComparison.Ordinal) && objective.EndTick is not null)
        {
            errors.Add("scenarioObjective endTick must be empty while in progress");
        }

        if (!string.Equals(objective.EndReason, "in_progress", StringComparison.Ordinal))
        {
            if (objective.EndTick is null)
            {
                errors.Add("scenarioObjective endTick must be present after completion");
            }
            else if (objective.EndTick < objective.StartedTick)
            {
                errors.Add("scenarioObjective endTick must not be before startedTick");
            }
        }

        ValidateScenarioList("completedCharterIds", objective.CompletedCharterIds, errors);
        ValidateScenarioList("completedCharterResourceIds", objective.CompletedCharterResourceIds, errors);

        if (objective.StableNeedStreaks is null)
        {
            errors.Add("scenarioObjective stableNeedStreaks must not be null");
        }
        else
        {
            var seenStableNeedKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var (key, streak) in objective.StableNeedStreaks)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    errors.Add("scenarioObjective stableNeedStreaks key must not be empty");
                }
                else
                {
                    var trimmed = key.Trim();
                    if (!string.Equals(key, trimmed, StringComparison.Ordinal))
                    {
                        errors.Add($"scenarioObjective stableNeedStreaks '{key}' must not contain surrounding whitespace");
                    }

                    if (!seenStableNeedKeys.Add(trimmed))
                    {
                        errors.Add($"scenarioObjective stableNeedStreaks '{trimmed}' must not be duplicated");
                    }
                }

                if (streak < 0)
                {
                    errors.Add($"scenarioObjective stableNeedStreaks '{key}' must not be negative");
                }
            }
        }

        if (objective.FinalScore is < 0 or > 100)
        {
            errors.Add("scenarioObjective finalScore must be between 0 and 100");
        }
    }

    private static void ValidateScenarioList(string name, IReadOnlyList<string>? values, List<string> errors)
    {
        if (values is null)
        {
            errors.Add($"scenarioObjective {name} must not be null");
            return;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add($"scenarioObjective {name} entries must not be empty");
                continue;
            }

            var trimmed = value.Trim();
            if (!string.Equals(value, trimmed, StringComparison.Ordinal))
            {
                errors.Add($"scenarioObjective {name} '{value}' must not contain surrounding whitespace");
            }

            if (!seen.Add(trimmed))
            {
                errors.Add($"scenarioObjective {name} '{trimmed}' must not be duplicated");
            }
        }
    }
}
