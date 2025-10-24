using IdleGatherWebGame.Models;
using System.Collections.Generic;

namespace IdleGatherWebGame.Services;

public interface ISkillService
{
    IReadOnlyDictionary<string, Skill> All { get; }

    // Ensure a known list of skills exists with names (id -> name).
    void EnsureKnownSkills(IEnumerable<(string id, string name)> skills);

    Skill Get(string id);

    // Persistence helpers:
    Dictionary<string, PlayerData.SkillData> ToSaveDictionary();
    void LoadFromDictionary(Dictionary<string, PlayerData.SkillData> saved);
}
