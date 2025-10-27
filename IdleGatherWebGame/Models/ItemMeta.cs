namespace IdleGatherWebGame.Models;

public sealed record ItemMeta(
    string Id,
    string Name,
    string Category, // e.g. "Resources", "Materials", "Currencies"
    string Icon,       // emoji or URL/data-URL
    int? SellPricePerUnit = null
);

public static class ItemRegistry
{
    // Single source of truth
    private static readonly Dictionary<string, ItemMeta> _byId = new()
    {
        // Currencies
        ["coins"] = new("coins", "Coins", "Currencies", "🪙", null), //Main Currency
        ["chips"] = new("chips", "Chips", "Currencies", "🎰", null), //Casino Currency
        // Logs
        ["log_t1"] = new("log_t1", "Log (T1)", "Resources", "🌲", 2),
        ["log_t2"] = new("log_t2", "Log (T2)", "Resources", "🌲", 3),
        ["log_t3"] = new("log_t3", "Log (T3)", "Resources", "🌲", 4),
        ["log_t4"] = new("log_t4", "Log (T4)", "Resources", "🌲", 6),
        ["log_t5"] = new("log_t5", "Log (T5)", "Resources", "🌲", 8),
        ["log_t6"] = new("log_t6", "Log (T6)", "Resources", "🌲", 10),
        ["log_t7"] = new("log_t7", "Log (T7)", "Resources", "🌲", 14),
        // Planks
        ["plank_t1"] = new("plank_t1", "Plank (T1)", "Materials", "🪵", 5),
        ["plank_t2"] = new("plank_t2", "Plank (T2)", "Materials", "🪵", 7),
        ["plank_t3"] = new("plank_t3", "Plank (T3)", "Materials", "🪵", 10),
        ["plank_t4"] = new("plank_t4", "Plank (T4)", "Materials", "🪵", 14),
        ["plank_t5"] = new("plank_t5", "Plank (T5)", "Materials", "🪵", 18),
        ["plank_t6"] = new("plank_t6", "Plank (T6)", "Materials", "🪵", 22),
        ["plank_t7"] = new("plank_t7", "Plank (T7)", "Materials", "🪵", 30),
        // Ores
        ["copper_ore"] = new("copper_ore", "Copper Ore", "Resources", "🪨", 3),
        ["tin_ore"] = new("tin_ore", "Tin Ore", "Resources", "🪨", 3),
        ["iron_ore"] = new("iron_ore", "Iron Ore", "Resources", "🪨", 4),
        ["silver_ore"] = new("silver_ore", "Silver Ore", "Resources", "🪨", 6),
        ["gold_ore"] = new("gold_ore", "Gold Ore", "Resources", "🪨", 8),
        ["mithril_ore"] = new("mithril_ore", "Mithril Ore", "Resources", "🪨", 10),
        ["adamant_ore"] = new("adamant_ore", "Adamantite Ore", "Resources", "🪨", 12),
        // Bars
        ["bronze_bar"] = new("bronze_bar", "Bronze Bar", "Bars", "🔩", 10),
        ["iron_bar"] = new("iron_bar", "Iron Bar", "Bars", "🔩", 14),
        ["silver_bar"] = new("silver_bar", "Silver Bar", "Bars", "🔩", 20),
        ["gold_bar"] = new("gold_bar", "Gold Bar", "Bars", "🔩", 30),
        ["mith_bar"] = new("mith_bar", "Mithril Bar", "Bars", "🔩", 40),
        ["adam_bar"] = new("adam_bar", "Adamant Bar", "Bars", "🔩", 55),
    };

    public static bool TryGet(string id, out ItemMeta meta) => _byId.TryGetValue(id, out meta);
    public static IEnumerable<ItemMeta> All => _byId.Values;
}
