using System.Collections.Generic;

namespace IdleGatherWebGame.Models;

public sealed record OreDef(
    string Id,
    string Name,
    string Icon,
    int RequiredLevel,
    double XpPerCycle,
    (double min, double max) YieldRange,
    double DurationSeconds
);

public static class OreRegistry
{
    private static readonly Dictionary<string, OreDef> _byId = new()
    {
        ["copper_ore"] = new("copper_ore", "Copper Ore", "⛏️", 1, 8.0, (1.0, 3.0), 5.0),
        ["tin_ore"] = new("tin_ore", "Tin Ore", "⛏️", 5, 10.0, (1.0, 3.0), 6.0),
        ["iron_ore"] = new("iron_ore", "Iron Ore", "⛏️", 10, 12.0, (1.0, 3.0), 7.0),
        ["silver_ore"] = new("silver_ore", "Silver Ore", "⛏️", 15, 16.0, (1.0, 3.0), 8.0),
        ["gold_ore"] = new("gold_ore", "Gold Ore", "⛏️", 20, 22.0, (1.0, 3.0), 9.0),
        ["mithril_ore"] = new("mithril_ore", "Mithril Ore", "⛏️", 30, 30.0, (1.0, 3.0), 10.0),
        ["adamant_ore"] = new("adamant_ore", "Adamantite Ore", "⛏️", 40, 40.0, (1.0, 3.0), 12.0),
    };

    public static IEnumerable<OreDef> All => _byId.Values;
    public static bool TryGet(string id, out OreDef def) => _byId.TryGetValue(id, out def);
}
