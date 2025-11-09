using System;
using System.Collections.Generic;
using IdleGatherWebGame.Services;

namespace IdleGatherWebGame.Models;

public static class RecipeRegistry
{
    public static readonly List<GameState.CraftRecipe> Crafting = new()
    {
        new GameState.CraftRecipe
        {
            Id = "plank_t1",
            Name = "Plank (T1)",
            Icon = "🪵",
            RequiredLevel = 1,
            Duration = TimeSpan.FromSeconds(4),
            Inputs = [ new("log_t1", 2) ],
            Outputs = [ new("plank_t1", 1) ],
            XpPerCycle = 6
        },
        new GameState.CraftRecipe
        {
            Id = "plank_t2",
            Name = "Plank (T2)",
            Icon = "🪵",
            RequiredLevel = 5,
            Duration = TimeSpan.FromSeconds(5),
            Inputs = [ new("log_t2", 2) ],
            Outputs = [ new("plank_t2", 1) ],
            XpPerCycle = 10
        },
        new GameState.CraftRecipe
        {
            Id = "plank_t3",
            Name = "Plank (T3)",
            Icon = "🪵",
            RequiredLevel = 10,
            Duration = TimeSpan.FromSeconds(6),
            Inputs = [ new("log_t3", 2) ],
            Outputs = [ new("plank_t3", 1) ],
            XpPerCycle = 14
        },
        new GameState.CraftRecipe
        {
            Id = "plank_t4",
            Name = "Plank (T4)",
            Icon = "🪵",
            RequiredLevel = 15,
            Duration = TimeSpan.FromSeconds(7),
            Inputs = [ new("log_t4", 2) ],
            Outputs = [ new("plank_t4", 1) ],
            XpPerCycle = 18
        },
        new GameState.CraftRecipe
        {
            Id = "plank_t5",
            Name = "Plank (T5)",
            Icon = "🪵",
            RequiredLevel = 20,
            Duration = TimeSpan.FromSeconds(8),
            Inputs = [ new("log_t5", 2) ],
            Outputs = [ new("plank_t5", 1) ],
            XpPerCycle = 22
        },
        new GameState.CraftRecipe
        {
            Id = "plank_t6",
            Name = "Plank (T6)",
            Icon = "🪵",
            RequiredLevel = 25,
            Duration = TimeSpan.FromSeconds(9),
            Inputs = [ new("log_t6", 2) ],
            Outputs = [ new("plank_t6", 1) ],
            XpPerCycle = 26
        },
        new GameState.CraftRecipe
        {
            Id = "plank_t7",
            Name = "Plank (T7)",
            Icon = "🪵",
            RequiredLevel = 30,
            Duration = TimeSpan.FromSeconds(10),
            Inputs = [ new("log_t7", 2) ],
            Outputs = [ new("plank_t7", 1) ],
            XpPerCycle = 30
        },
    };

    public static readonly List<GameState.CraftRecipe> Smelting = new()
    {
        new GameState.CraftRecipe
        {
            Id = "bronze_bar",
            Name = "Bronze Bar",
            Icon = "🔩",
            RequiredLevel = 1,
            Duration = TimeSpan.FromSeconds(4),
            Inputs = new() { new("copper_ore", 1), new("tin_ore", 1) },
            Outputs = new() { new("bronze_bar", 1) },
            XpPerCycle = 8
        },
        new GameState.CraftRecipe
        {
            Id = "iron_bar",
            Name = "Iron Bar",
            Icon = "🔩",
            RequiredLevel = 8,
            Duration = TimeSpan.FromSeconds(4),
            Inputs = [ new("iron_ore", 2) ],
            Outputs = [ new("iron_bar", 1) ],
            XpPerCycle = 12
        },
        new GameState.CraftRecipe
        {
            Id = "silver_bar",
            Name = "Silver Bar",
            Icon = "🔩",
            RequiredLevel = 15,
            Duration = TimeSpan.FromSeconds(4),
            Inputs = [ new("silver_ore", 2) ],
            Outputs = [ new("silver_bar", 1) ],
            XpPerCycle = 16
        },
        new GameState.CraftRecipe
        {
            Id = "gold_bar",
            Name = "Gold Bar",
            Icon = "🔩",
            RequiredLevel = 20,
            Duration = TimeSpan.FromSeconds(4),
            Inputs = [ new("gold_ore", 2) ],
            Outputs = [ new("gold_bar", 1) ],
            XpPerCycle = 20
        },
        new GameState.CraftRecipe
        {
            Id = "mith_bar",
            Name = "Mithril Bar",
            Icon = "🔩",
            RequiredLevel = 30,
            Duration = TimeSpan.FromSeconds(4),
            Inputs = [ new("mithril_ore", 3) ],
            Outputs = [ new("mith_bar", 1) ],
            XpPerCycle = 28
        },
        new GameState.CraftRecipe
        {
            Id = "adam_bar",
            Name = "Adamant Bar",
            Icon = "🔩",
            RequiredLevel = 40,
            Duration = TimeSpan.FromSeconds(4),
            Inputs = [ new("adamant_ore", 3) ],
            Outputs = [ new("adam_bar", 1) ],
            XpPerCycle = 34
        },
    };
    public static readonly List<GameState.CraftRecipe> Tools = new()
{
    new GameState.CraftRecipe
    {
        Id = "axe_t1",
        Name = "Axe (T1)",
        Icon = "🪓",
        RequiredLevel = 1,
        Duration = TimeSpan.FromSeconds(8),
        Inputs = new()
        {
            new("plank_t1", 12),
            new("copper_ore", 6)
        },
        Outputs = new() { new("axe_t1", 1) },
        XpPerCycle = 15
    },
        new GameState.CraftRecipe
    {
        Id = "axe_t1",
        Name = "Axe (T1)",
        Icon = "🪓",
        RequiredLevel = 1,
        Duration = TimeSpan.FromSeconds(8),
        Inputs = new() { new("plank_t1", 12), new("copper_ore", 6) },
        Outputs = new() { new("axe_t1", 1) },
        XpPerCycle = 15
    },
    new GameState.CraftRecipe
    {
        Id = "axe_t2",
        Name = "Axe (T2)",
        Icon = "🪓",
        RequiredLevel = 8,
        Duration = TimeSpan.FromSeconds(10),
        Inputs = new() { new("plank_t2", 15), new("bronze_bar", 4) },
        Outputs = new() { new("axe_t2", 1) },
        XpPerCycle = 25
    },
    new GameState.CraftRecipe
    {
        Id = "axe_t3",
        Name = "Axe (T3)",
        Icon = "🪓",
        RequiredLevel = 15,
        Duration = TimeSpan.FromSeconds(12),
        Inputs = new() { new("plank_t3", 18), new("iron_bar", 5) },
        Outputs = new() { new("axe_t3", 1) },
        XpPerCycle = 40
    },
    new GameState.CraftRecipe
    {
        Id = "axe_t4",
        Name = "Axe (T4)",
        Icon = "🪓",
        RequiredLevel = 25,
        Duration = TimeSpan.FromSeconds(15),
        Inputs = new() { new("plank_t4", 22), new("silver_bar", 6) },
        Outputs = new() { new("axe_t4", 1) },
        XpPerCycle = 60
    },

    // ===== CRAFTING TOOLS (Saws) =====
    new GameState.CraftRecipe
    {
        Id = "saw_t1",
        Name = "Saw (T1)",
        Icon = "🪚",
        RequiredLevel = 1,
        Duration = TimeSpan.FromSeconds(8),
        Inputs = new() { new("plank_t1", 10), new("copper_ore", 8) },
        Outputs = new() { new("saw_t1", 1) },
        XpPerCycle = 15
    },
    new GameState.CraftRecipe
    {
        Id = "saw_t2",
        Name = "Saw (T2)",
        Icon = "🪚",
        RequiredLevel = 8,
        Duration = TimeSpan.FromSeconds(10),
        Inputs = new() { new("plank_t2", 12), new("bronze_bar", 5) },
        Outputs = new() { new("saw_t2", 1) },
        XpPerCycle = 25
    },
    new GameState.CraftRecipe
    {
        Id = "saw_t3",
        Name = "Saw (T3)",
        Icon = "🪚",
        RequiredLevel = 15,
        Duration = TimeSpan.FromSeconds(12),
        Inputs = new() { new("plank_t3", 15), new("iron_bar", 6) },
        Outputs = new() { new("saw_t3", 1) },
        XpPerCycle = 40
    },
    new GameState.CraftRecipe
    {
        Id = "saw_t4",
        Name = "Saw (T4)",
        Icon = "🪚",
        RequiredLevel = 25,
        Duration = TimeSpan.FromSeconds(15),
        Inputs = new() { new("plank_t4", 18), new("silver_bar", 7) },
        Outputs = new() { new("saw_t4", 1) },
        XpPerCycle = 60
    },

    // ===== MINING TOOLS (Pickaxes) =====
    new GameState.CraftRecipe
    {
        Id = "pickaxe_t1",
        Name = "Pickaxe (T1)",
        Icon = "⛏️",
        RequiredLevel = 1,
        Duration = TimeSpan.FromSeconds(8),
        Inputs = new() { new("plank_t1", 8), new("copper_ore", 12) },
        Outputs = new() { new("pickaxe_t1", 1) },
        XpPerCycle = 15
    },
    new GameState.CraftRecipe
    {
        Id = "pickaxe_t2",
        Name = "Pickaxe (T2)",
        Icon = "⛏️",
        RequiredLevel = 8,
        Duration = TimeSpan.FromSeconds(10),
        Inputs = new() { new("plank_t2", 10), new("bronze_bar", 6) },
        Outputs = new() { new("pickaxe_t2", 1) },
        XpPerCycle = 25
    },
    new GameState.CraftRecipe
    {
        Id = "pickaxe_t3",
        Name = "Pickaxe (T3)",
        Icon = "⛏️",
        RequiredLevel = 15,
        Duration = TimeSpan.FromSeconds(12),
        Inputs = new() { new("plank_t3", 12), new("iron_bar", 8) },
        Outputs = new() { new("pickaxe_t3", 1) },
        XpPerCycle = 40
    },
    new GameState.CraftRecipe
    {
        Id = "pickaxe_t4",
        Name = "Pickaxe (T4)",
        Icon = "⛏️",
        RequiredLevel = 25,
        Duration = TimeSpan.FromSeconds(15),
        Inputs = new() { new("plank_t4", 15), new("silver_bar", 9) },
        Outputs = new() { new("pickaxe_t4", 1) },
        XpPerCycle = 60
    },
};
}
