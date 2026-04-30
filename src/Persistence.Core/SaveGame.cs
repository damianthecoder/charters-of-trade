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
    string? PendingRouteContractId);

public static class SaveCodec
{
    public const int CurrentSaveVersion = 1;

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
}
