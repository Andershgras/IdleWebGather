using System;
using System.Collections.Generic;
using IdleGatherWebGame.Services; // For GameState.CraftRecipe

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
        // Add more tiers as needed (T3–T7)
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
