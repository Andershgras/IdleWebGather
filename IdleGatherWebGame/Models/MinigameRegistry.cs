public static class MinigameRegistry
{
    public sealed record Mini(string Id, string Name, string Icon = "🎯", string Blurb = "Skill-based score attack");

    public static readonly List<Mini> All = new()
    {
        new("aim_test", "Aim Test", "🎯", "Click targets quickly without missing"),
        new("reflex",    "Reflex",   "⚡", "Tap when the signal flashes"),
        new("path",      "Path",     "🧩", "Trace the path without errors")
    };
}
