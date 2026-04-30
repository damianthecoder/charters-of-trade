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
    string? PendingRouteContractId);

public static class SaveCodec
{
    public const int CurrentSaveVersion = 2;

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
            FogOfWar = save.FogOfWar with
            {
                DiscoveredNodes = save.FogOfWar.DiscoveredNodes.Order(StringComparer.Ordinal).ToArray()
            }
        };
    }

    private static string? NormalizeWarehousePolicyMode(string? mode)
    {
        return string.Equals(mode, "balanced", StringComparison.Ordinal)
            ? null
            : mode;
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

        if (save.PendingRouteContractId is not null && string.IsNullOrWhiteSpace(save.PendingRouteContractId))
        {
            errors.Add("pendingRouteContractId must not be empty when present");
        }

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
}
