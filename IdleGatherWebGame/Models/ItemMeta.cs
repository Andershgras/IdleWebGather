namespace IdleGatherWebGame.Models;

public sealed record ItemMeta(
    string Id,
    string Name,
    string Category,   // e.g. "Resources", "Materials", "Currencies"
    string Icon,       // emoji or URL/data-URL
    int? SellPricePerUnit = null
);

public static class ItemRegistry
{
    // Single source of truth (extend as you add content)
    private static readonly Dictionary<string, ItemMeta> _byId = new()
    {
        ["wood"] = new("wood", "Log", "Resources", "🌲", 2),
        ["stone"] = new("stone", "Stone", "Resources", "🪨", null),
        ["keys"] = new("keys", "Keys", "Currencies", "🗝️", null),
        ["coins"] = new("coins", "Coins", "Currencies", "🪙", null),
        ["plank_t1"] = new("plank_t1", "Plank (T1)", "Materials", "🪵", 5),
    };

    public static bool TryGet(string id, out ItemMeta meta) => _byId.TryGetValue(id, out meta);
    public static IEnumerable<ItemMeta> All => _byId.Values;
}
