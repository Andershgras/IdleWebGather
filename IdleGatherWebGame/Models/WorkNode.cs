namespace IdleGatherWebGame.Models;
public class WorkNode
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "❓";
    public int RequiredLevel { get; set; }
    public double XpPerCycle { get; set; }
    public double MinYield { get; set; }
    public double MaxYield { get; set; }
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(5);
}
