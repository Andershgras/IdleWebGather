using System;
using System.Collections.Generic;

namespace IdleGatherWebGame.Models
{
    public class PlayerData
    {
        public string Name { get; set; } = "";
        public int SaveVersion { get; set; } = 0;
        public Dictionary<string, double> Resources { get; set; } = [];
        public Dictionary<string, SkillData> Skills { get; set; } = [];
        public ActiveJobSave? ActiveJob { get; set; }
        public DateTimeOffset LastSavedUtc { get; set; } = DateTimeOffset.UtcNow;
        public int OverallLevel { get; set; } = 1;
        public double OverallXp { get; set; } = 0;
        public int TotalClicks { get; set; } = 0;
        public Dictionary<string, double> MinigameHighScores { get; set; } = new();
        public int MinigameGamesPlayed { get; set; } = 0;
        public Dictionary<string, string> Equipment { get; set; } = new();


        public class SkillData
        {
            public int Level { get; set; } = 1;
            public double Xp { get; set; } = 0;
        }

        public class ActiveJobSave
        {
            public string? NodeId { get; set; } = "";
            public double ElapsedSeconds { get; set; }
            public string? RecipeId { get; set; }
        }
    }
}
