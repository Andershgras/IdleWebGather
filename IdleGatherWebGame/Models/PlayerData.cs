using System;
using System.Collections.Generic;

namespace IdleGatherWebGame.Models
{
    public class PlayerData
    {
        public string Name { get; set; } = "";
        public int SaveVersion { get; set; } = 0;
        // id -> amount
        public Dictionary<string, double> Resources { get; set; } = new();

        // skill id -> data
        public Dictionary<string, SkillData> Skills { get; set; } = new();

        public ActiveJobSave? ActiveJob { get; set; }

        public DateTimeOffset LastSavedUtc { get; set; } = DateTimeOffset.UtcNow;

        // === NEW: Overall player progression (DTO only; no methods here) ===
        public int OverallLevel { get; set; } = 1;
        public double OverallXp { get; set; } = 0;

        public class SkillData
        {
            public int Level { get; set; } = 1;
            public double Xp { get; set; } = 0;
        }

        public class ActiveJobSave
        {
            public string NodeId { get; set; } = "";
            public double ElapsedSeconds { get; set; }
            public string? RecipeId { get; set; }
        }
    }
}
