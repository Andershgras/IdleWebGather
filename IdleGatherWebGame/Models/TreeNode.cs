namespace IdleGatherWebGame.Models;

public class TreeNode
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Icon { get; init; } = "🌳";
    public int RequiredLevel { get; init; } = 1;
    public double MinLogs { get; init; } = 1;
    public double MaxLogs { get; init; } = 2;
    public double XpPerCycle { get; init; } = 6.5;
    public TimeSpan Duration { get; init; } = TimeSpan.FromSeconds(6);
}
