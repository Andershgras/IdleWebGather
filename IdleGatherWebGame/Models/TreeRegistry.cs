using System.Collections.Generic;

namespace IdleGatherWebGame.Models;

public sealed record TreeDef(
    string Id,
    string Name,
    string Icon,
    int RequiredLevel,
    double XpPerCycle,
    double MinLogs,
    double MaxLogs,
    double DurationSeconds
);

public static class TreeRegistry
{
    private static readonly Dictionary<string, TreeDef> _byId = new()
    {
        ["tree_1"] = new("tree_1", "Shrub", "🌳", 1, 6.0, 1, 2, 6),
        ["tree_2"] = new("tree_2", "Oak", "🌳", 5, 8.0, 1, 2, 6),
        ["tree_3"] = new("tree_3", "Willow", "🌳", 10, 10.0, 1, 2, 6),
        ["tree_4"] = new("tree_4", "Maple", "🌳", 15, 12.0, 1, 2, 6),
        ["tree_5"] = new("tree_5", "Yew", "🌳", 20, 14.0, 1, 2, 6),
        ["tree_6"] = new("tree_6", "Magic", "🌳", 30, 18.0, 1, 2, 6),
        ["tree_7"] = new("tree_7", "Elder", "🌳", 40, 22.0, 1, 2, 6),
    };

    public static IEnumerable<TreeDef> All => _byId.Values;
    public static bool TryGet(string id, out TreeDef def) => _byId.TryGetValue(id, out def);
}
