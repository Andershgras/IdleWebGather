using IdleGatherWebGame.Models;
using System;
using System.Collections.Generic;

namespace IdleGatherWebGame.Services;

public sealed class SkillService : ISkillService
{
    private readonly Dictionary<string, Skill> _skills = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, Skill> All => _skills;

    public void EnsureKnownSkills(IEnumerable<(string id, string name)> skills)
    {
        foreach (var (id, name) in skills)
        {
            if (!_skills.ContainsKey(id))
                _skills[id] = new Skill(id, name);
        }
    }

    public Skill Get(string id)
    {
        if (!_skills.TryGetValue(id, out var s))
        {
            // Lazy-safe fallback: create with id as name
            s = new Skill(id, id);
            _skills[id] = s;
        }
        return s;
    }

    public Dictionary<string, PlayerData.SkillData> ToSaveDictionary()
    {
        var dict = new Dictionary<string, PlayerData.SkillData>(StringComparer.OrdinalIgnoreCase);
        foreach (var (id, s) in _skills)
            dict[id] = new PlayerData.SkillData { Level = s.Level, Xp = s.Xp };
        return dict;
    }

    public void LoadFromDictionary(Dictionary<string, PlayerData.SkillData> saved)
    {
        if (saved is null) return;

        foreach (var (id, data) in saved)
        {
            var s = Get(id);        // ensure it exists
            s.Restore(data.Level, data.Xp);
        }
    }
}
