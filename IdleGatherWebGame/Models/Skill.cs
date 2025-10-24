namespace IdleGatherWebGame.Models;

public class Skill
{
    public string Id { get; }
    public string Name { get; }
    public int Level { get; private set; } = 1;
    public double Xp { get; private set; } = 0;

    public Skill(string id, string name) { Id = id; Name = name; }

    public double XpForNextLevel => 50 * Math.Pow(1.35, Level - 1); // simple curve

    public void AddXp(double amount)
    {
        Xp += amount;
        while (Xp >= XpForNextLevel)
        {
            Xp -= XpForNextLevel;
            Level++;
        }
    }
}
