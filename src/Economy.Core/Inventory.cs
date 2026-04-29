namespace ChartersOfTrade.Economy.Core;

public sealed class Inventory
{
    private readonly Dictionary<string, int> _stock;

    public Inventory()
    {
        _stock = new Dictionary<string, int>(StringComparer.Ordinal);
    }

    public Inventory(IDictionary<string, int> stock)
    {
        _stock = new Dictionary<string, int>(stock, StringComparer.Ordinal);
        foreach (var (resourceId, amount) in _stock)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
            {
                throw new ArgumentException("Inventory resource id must not be empty.", nameof(stock));
            }

            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stock), $"Inventory resource '{resourceId}' has negative stock.");
            }
        }
    }

    public IReadOnlyDictionary<string, int> Stock => _stock;

    public int Get(string resourceId)
    {
        return _stock.TryGetValue(resourceId, out var amount) ? amount : 0;
    }

    public void Add(string resourceId, int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        _stock[resourceId] = Get(resourceId) + amount;
    }

    public bool TryRemove(string resourceId, int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        var current = Get(resourceId);
        if (current < amount)
        {
            return false;
        }

        _stock[resourceId] = current - amount;
        return true;
    }

    public Dictionary<string, int> ToDictionary()
    {
        return _stock.OrderBy(kvp => kvp.Key, StringComparer.Ordinal).ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal);
    }
}
