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

public sealed record WarehousePolicySaveState(
    string CityId,
    string ResourceId,
    bool ReorderEnabled,
    int ReserveStock);

public sealed record RoutePolicySaveState(
    string RouteId,
    IReadOnlyList<string> ReservedResources,
    string? PriorityResourceId);

public sealed record EventSaveState(string Id, string State, int DaysRemaining);

public sealed record FogOfWarState(IReadOnlyList<string> DiscoveredNodes);

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
    IReadOnlyList<WarehousePolicySaveState>? WarehousePolicies,
    IReadOnlyList<RoutePolicySaveState>? RoutePolicies,
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
            WarehousePolicies = (save.WarehousePolicies ?? [])
                .OrderBy(policy => policy.CityId, StringComparer.Ordinal)
                .ThenBy(policy => policy.ResourceId, StringComparer.Ordinal)
                .ToArray(),
            RoutePolicies = (save.RoutePolicies ?? [])
                .Select(policy => policy with
                {
                    ReservedResources = (policy.ReservedResources ?? []).Order(StringComparer.Ordinal).ToArray()
                })
                .OrderBy(policy => policy.RouteId, StringComparer.Ordinal)
                .ToArray(),
            FogOfWar = save.FogOfWar with
            {
                DiscoveredNodes = save.FogOfWar.DiscoveredNodes.Order(StringComparer.Ordinal).ToArray()
            }
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

        if (save.SaveVersion <= 0)
        {
            errors.Add("saveVersion must be positive");
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

        foreach (var route in save.Routes)
        {
            if (route.CapacityPerDay <= 0)
            {
                errors.Add($"route '{route.Id}' capacityPerDay must be positive");
            }
        }

        ValidateWarehousePolicies(save.WarehousePolicies ?? [], errors);
        ValidateRoutePolicies(save.RoutePolicies ?? [], errors);

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

    private static void ValidateWarehousePolicies(IReadOnlyList<WarehousePolicySaveState> policies, List<string> errors)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var policy in policies)
        {
            if (string.IsNullOrWhiteSpace(policy.CityId))
            {
                errors.Add("warehouse policy cityId must not be empty");
            }

            if (string.IsNullOrWhiteSpace(policy.ResourceId))
            {
                errors.Add("warehouse policy resourceId must not be empty");
            }

            if (!keys.Add($"{policy.CityId}\u001f{policy.ResourceId}"))
            {
                errors.Add($"duplicate warehouse policy for city '{policy.CityId}' and resource '{policy.ResourceId}'");
            }

            if (policy.ReserveStock < 0)
            {
                errors.Add($"warehouse policy for city '{policy.CityId}' resource '{policy.ResourceId}' reserveStock must not be negative");
            }
        }
    }

    private static void ValidateRoutePolicies(IReadOnlyList<RoutePolicySaveState> policies, List<string> errors)
    {
        var routeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var policy in policies)
        {
            if (string.IsNullOrWhiteSpace(policy.RouteId))
            {
                errors.Add("route policy routeId must not be empty");
            }

            if (!routeIds.Add(policy.RouteId))
            {
                errors.Add($"duplicate route policy for route '{policy.RouteId}'");
            }

            if (policy.ReservedResources is null)
            {
                errors.Add($"route policy '{policy.RouteId}' reservedResources must not be null");
                continue;
            }

            var resources = new HashSet<string>(StringComparer.Ordinal);
            foreach (var resourceId in policy.ReservedResources)
            {
                if (string.IsNullOrWhiteSpace(resourceId))
                {
                    errors.Add($"route policy '{policy.RouteId}' reserved resource id must not be empty");
                }

                if (!resources.Add(resourceId))
                {
                    errors.Add($"route policy '{policy.RouteId}' has duplicate reserved resource '{resourceId}'");
                }
            }

            if (policy.PriorityResourceId is not null && string.IsNullOrWhiteSpace(policy.PriorityResourceId))
            {
                errors.Add($"route policy '{policy.RouteId}' priorityResourceId must not be empty when present");
            }

            if (policy.PriorityResourceId is not null && !resources.Contains(policy.PriorityResourceId))
            {
                errors.Add($"route policy '{policy.RouteId}' priorityResourceId must be one of reservedResources");
            }
        }
    }
}
