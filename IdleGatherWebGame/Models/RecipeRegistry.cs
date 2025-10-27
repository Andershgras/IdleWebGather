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
            Inputs = new() { new("log_t1", 2) },
            Outputs = new() { new("plank_t1", 1) },
            XpPerCycle = 6
        },
        new GameState.CraftRecipe
        {
            Id = "plank_t2",
            Name = "Plank (T2)",
            Icon = "🪵",
            RequiredLevel = 5,
            Duration = TimeSpan.FromSeconds(5),
            Inputs = new() { new("log_t2", 2) },
            Outputs = new() { new("plank_t2", 1) },
            XpPerCycle = 10
        },
        new GameState.CraftRecipe
        {
            Id = "plank_t3",
            Name = "Plank (T3)",
            Icon = "🪵",
            RequiredLevel = 10,
            Duration = TimeSpan.FromSeconds(6),
            Inputs = new() { new("log_t3", 2) },
            Outputs = new() { new("plank_t3", 1) },
            XpPerCycle = 14
        },
        new GameState.CraftRecipe
        {
            Id = "plank_t4",
            Name = "Plank (T4)",
            Icon = "🪵",
            RequiredLevel = 15,
            Duration = TimeSpan.FromSeconds(7),
            Inputs = new() { new("log_t4", 2) },
            Outputs = new() { new("plank_t4", 1) },
            XpPerCycle = 18
        },
        new GameState.CraftRecipe
        {
            Id = "plank_t5",
            Name = "Plank (T5)",
            Icon = "🪵",
            RequiredLevel = 20,
            Duration = TimeSpan.FromSeconds(8),
            Inputs = new() { new("log_t5", 2) },
            Outputs = new() { new("plank_t5", 1) },
            XpPerCycle = 22
        },
        new GameState.CraftRecipe
        {
            Id = "plank_t6",
            Name = "Plank (T6)",
            Icon = "🪵",
            RequiredLevel = 25,
            Duration = TimeSpan.FromSeconds(9),
            Inputs = new() { new("log_t6", 2) },
            Outputs = new() { new("plank_t6", 1) },
            XpPerCycle = 26
        },
        new GameState.CraftRecipe
        {
            Id = "plank_t7",
            Name = "Plank (T7)",
            Icon = "🪵",
            RequiredLevel = 30,
            Duration = TimeSpan.FromSeconds(10),
            Inputs = new() { new("log_t7", 2) },
            Outputs = new() { new("plank_t7", 1) },
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
            Inputs = new() { new("iron_ore", 2) },
            Outputs = new() { new("iron_bar", 1) },
            XpPerCycle = 12
        },
        new GameState.CraftRecipe
        {
            Id = "silver_bar",
            Name = "Silver Bar",
            Icon = "🔩",
            RequiredLevel = 15,
            Duration = TimeSpan.FromSeconds(4),
            Inputs = new() { new("silver_ore", 2) },
            Outputs = new() { new("silver_bar", 1) },
            XpPerCycle = 16
        },
        new GameState.CraftRecipe
        {
            Id = "gold_bar",
            Name = "Gold Bar",
            Icon = "🔩",
            RequiredLevel = 20,
            Duration = TimeSpan.FromSeconds(4),
            Inputs = new() { new("gold_ore", 2) },
            Outputs = new() { new("gold_bar", 1) },
            XpPerCycle = 20
        },
        new GameState.CraftRecipe
        {
            Id = "mith_bar",
            Name = "Mithril Bar",
            Icon = "🔩",
            RequiredLevel = 30,
            Duration = TimeSpan.FromSeconds(4),
            Inputs = new() { new("mithril_ore", 3) },
            Outputs = new() { new("mith_bar", 1) },
            XpPerCycle = 28
        },
        new GameState.CraftRecipe
        {
            Id = "adam_bar",
            Name = "Adamant Bar",
            Icon = "🔩",
            RequiredLevel = 40,
            Duration = TimeSpan.FromSeconds(4),
            Inputs = new() { new("adamant_ore", 3) },
            Outputs = new() { new("adam_bar", 1) },
            XpPerCycle = 34
        },
    };
}
