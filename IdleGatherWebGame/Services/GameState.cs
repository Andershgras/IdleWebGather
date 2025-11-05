using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Xml.Linq;
using IdleGatherWebGame.Models;

namespace IdleGatherWebGame.Services
{
    public sealed class GameState : IDisposable
    {
        public sealed record Toast(string Icon, string Text, DateTimeOffset ExpireAt);
        public int OverallLevel { get; private set; } = 1;
        public double OverallXp { get; private set; } = 0;
        public ISkillService Skills { get; }
        public double OverallXpNeededThisLevel => RequiredOverallXp(OverallLevel);
        public double OverallXpToNextLevel => Math.Max(0, OverallXpNeededThisLevel - OverallXp);
        private const int CurrentSaveVersion = 1;
        private const double CasinoHouseEdge = 0.02; // House edge for casino games (percentage of losing bias)
        private readonly IBrowserStorage? _storage;
        private enum JobType { Woodcutting, Mining, Crafting, Smelting }
        public bool AutosaveEnabled { get; set; } = true;
        public event Action? OnChange;
        public string? PlayerName { get; private set; }
        public IReadOnlyDictionary<string, Resource> Resources => _resources;
        public IReadOnlyList<WorkNode> TreeNodes => _trees;
        public IReadOnlyList<WorkNode> OreNodes => _ores;
        public IReadOnlyList<CraftRecipe> CraftingRecipes => _recipes;
        public IReadOnlyList<CraftRecipe> SmeltingRecipes => _smelt;
        #region Skill ShortCuts
        public Skill Clicking => Skills.Get(SkillIds.Clicking);
        public Skill Minigames => Skills.Get(SkillIds.Minigames);
        public Skill Woodcutting => Skills.Get(SkillIds.Woodcutting);
        public Skill Crafting => Skills.Get(SkillIds.Crafting);
        public Skill Mining => Skills.Get(SkillIds.Mining);
        public Skill Smelting => Skills.Get(SkillIds.Smelting);
        public Skill Casino => Skills.Get(SkillIds.Casino);
        public Skill Mastery => Skills.Get(SkillIds.Mastery);
        #endregion
        public bool JobRunning => _job is not null;
        public double Progress01 => _job?.Progress01 ?? 0;
        public double SecondsRemaining => _job is null ? 0 : Math.Max(0, (_job.Duration - _job.Elapsed).TotalSeconds);
        public int? ActiveCyclesRemaining => _job?.CyclesRemaining;
        public WorkNode? ActiveNode => _job?.Node;
        public CraftRecipe? ActiveRecipe => _job?.Recipe;
        public IReadOnlyList<Toast> Toasts => _toasts;
        public int DebugTicks { get; private set; }
        private const int TickMs = 200; // 5 ticks/sec for smoother bars
        private readonly List<Toast> _toasts = [];
        private Dictionary<string, Resource> _resources = [];
        private List<WorkNode> _trees = [];
        private List<WorkNode> _ores = [];
        private List<CraftRecipe> _recipes = [];
        private List<CraftRecipe> _smelt = [];
        private ActiveJob? _job;
        private readonly Timer _timer;
        public string? ActiveGatherNodeId => _job?.Node?.Id;
        public string? ActiveRecipeId => _job?.Recipe?.Id;
        private int _totalClicks = 0;               
        public int TotalClicks => _totalClicks;
        private Dictionary<string, double> _minigameHighScores = new(StringComparer.OrdinalIgnoreCase);
        private int _minigameGamesPlayed = 0;
        public IReadOnlyDictionary<string, double> MinigameHighScores => _minigameHighScores;
        public int MinigameGamesPlayed => _minigameGamesPlayed;
        public void Info(string icon, string message, double seconds = 2.5) => PushToast(icon, message, seconds);
        private readonly Dictionary<string, long> _masteryCounts = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _masteryTiers = new(StringComparer.OrdinalIgnoreCase);

        public sealed class CraftRecipe
        {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public string Icon { get; set; } = "🛠️";
            public int RequiredLevel { get; set; }
            public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(4);
            public List<Input> Inputs { get; set; } = [];
            public List<Output> Outputs { get; set; } = [];
            public double XpPerCycle { get; set; } = 5;
            public sealed record Input(string ResourceId, int Amount);
            public sealed record Output(string ResourceId, int Amount);
        }
        // Tiny data for generic gathering
        private sealed record GatherSpec(
            string Id,               // e.g., "woodcut:tree_1" or "mining:ore_1"
            WorkNode Node,           // timing, min/max yield, xp
            string OutputResourceId, // resource granted per cycle
            Skill Skill              // which skill receives XP
        );
        // ---------- Save/load ----------
        private const string SaveKey = "idleGather.save.v1";
        private bool _loadedFromLocal = false;
        private DateTime _lastSavedAt = DateTime.MinValue;
        private static readonly JsonSerializerOptions _json = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private sealed class ActiveJob
        {
            public JobType Type { get; }
            public WorkNode? Node { get; }
            public CraftRecipe? Recipe { get; }
            public GatherSpec? Gather { get; }
            public TimeSpan Duration { get; }
            public TimeSpan Elapsed { get; private set; }
            public int? CyclesRemaining { get; private set; } // null = infinite

            public ActiveJob(JobType type, WorkNode? node, CraftRecipe? recipe, GatherSpec? gather, int? cycles)
            {
                Type = type;
                Node = node;
                Recipe = recipe;
                Gather = gather;
                Duration = gather?.Node.Duration ?? node?.Duration ?? recipe?.Duration ?? TimeSpan.FromSeconds(5);
                CyclesRemaining = (cycles.HasValue && cycles.Value <= 0) ? null : cycles;
                Elapsed = TimeSpan.Zero;
            }
            public void Advance(TimeSpan dt) => Elapsed += dt;
            public bool Done => Elapsed >= Duration;

            public void ResetForNextCycle()
            {
                Elapsed = TimeSpan.Zero;
                if (CyclesRemaining.HasValue)
                    CyclesRemaining = Math.Max(0, CyclesRemaining.Value - 1);
            }

            public double Progress01 => Math.Clamp(Elapsed.TotalSeconds / Duration.TotalSeconds, 0, 1);
            public bool HasMoreCycles => !CyclesRemaining.HasValue || CyclesRemaining.Value > 0;
        }
        public string CurrentActivity => _job switch
        {
            null => "Doing nothing…",
            { Gather: not null, Type: JobType.Woodcutting } j => $"Chopping {j.Gather!.Node.Name}",
            { Gather: not null, Type: JobType.Mining } j => $"Mining {j.Gather!.Node.Name}",
            { Recipe: not null, Type: JobType.Crafting } j => $"Crafting {j.Recipe!.Name}",
            { Recipe: not null, Type: JobType.Smelting } j => $"Smelting {j.Recipe!.Name}",
            _ => "Working…"
        };

        public string ActiveOutputText => _job switch
        {
            { Gather: not null } j => $"{Math.Round(j.Gather!.Node.MinYield)}–{Math.Round(j.Gather!.Node.MaxYield)} " +
                                      $"{NiceUnitFor(j.Gather!.OutputResourceId)}, {Math.Round(j.Gather!.Node.XpPerCycle)} XP",
            { Recipe: not null } j => $"{string.Join(", ", j.Recipe!.Outputs.Select(o => $"{o.Amount} {IdToNice(o.ResourceId)}"))}, " +
                                      $"{Math.Round(j.Recipe!.XpPerCycle)} XP",
            _ => ""
        };
        public GameState(IBrowserStorage storage)
        {
            _storage = storage;
            // --- Initialize Skills ---
            Skills = new SkillService();
            Skills.EnsureKnownSkills([
                (SkillIds.Woodcutting, "Woodcutting"),
                (SkillIds.Crafting,    "Crafting"),
                (SkillIds.Mining,      "Mining"),
                (SkillIds.Smelting,    "Smelting"),
                (SkillIds.Casino,      "Casino"),
                (SkillIds.Clicking,    "Clicking"), 
                (SkillIds.Minigames,   "Minigames"),
                (SkillIds.Mastery,     "Mastery"),
            ]);

            InitDefaults();
            _timer = new Timer(_ => Tick(TimeSpan.FromMilliseconds(TickMs)), null, 250, TickMs);
        }
        public GameState() // fallback if DI ever calls parameterless
        {
            // --- Initialize Skills (new) ---
            Skills = new SkillService();
            Skills.EnsureKnownSkills([
                (SkillIds.Woodcutting, "Woodcutting"),
                (SkillIds.Crafting,    "Crafting"),
                (SkillIds.Mining,      "Mining"),
                (SkillIds.Smelting,    "Smelting"),
                (SkillIds.Casino,      "Casino"),
                (SkillIds.Clicking,    "Clicking"),
                (SkillIds.Minigames,   "Minigames"),
                (SkillIds.Mastery,     "Mastery"),
            ]);

            InitDefaults();
            _timer = new Timer(_ => Tick(TimeSpan.FromMilliseconds(TickMs)), null, 250, TickMs);
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public void CreateCharacter(string name)
        {
            name = (name ?? "").Trim();
            if (string.IsNullOrEmpty(name)) name = "Player";
            if (name.Length > 16) name = name.Substring(0, 16);
            PlayerName = name;
            OnChange?.Invoke();
        }

        // ---------- Public API: Start/Stop ----------
        public bool CanChop(WorkNode n) => Woodcutting.Level >= n.RequiredLevel;
        public bool CanMine(WorkNode n) => Mining.Level >= n.RequiredLevel;
        public bool CanCraft(CraftRecipe r) => Crafting.Level >= r.RequiredLevel;
        public bool CanSmelt(CraftRecipe r) => Smelting.Level >= r.RequiredLevel;

        public bool StartChop(WorkNode node, int? cycles)
        {
            if (PlayerName is null) return false;
            if (!CanChop(node)) return false;
            var spec = new GatherSpec($"woodcut:{node.Id}", node, TreeOutputId(node.Id), Woodcutting);

            _job = new ActiveJob(JobType.Woodcutting, node, recipe: null, gather: spec, cycles);
            OnChange?.Invoke();
            return true;
        }

        public bool StartMine(WorkNode node, int? cycles)
        {
            if (PlayerName is null) return false;
            if (!CanMine(node)) return false;
            var spec = new GatherSpec($"mining:{node.Id}", node, node.Id, Mining);
            _job = new ActiveJob(JobType.Mining, node, recipe: null, gather: spec, cycles);
            OnChange?.Invoke();
            return true;
        }

        public bool StartCraft(CraftRecipe r, int? cycles)
        {
            if (PlayerName is null) return false;
            if (!CanCraft(r)) return false;
            if (cycles.HasValue && cycles.Value <= 0) cycles = null;

            // must afford at least one cycle
            if (!HasInputsForOne(r)) return false;

            _job = new ActiveJob(JobType.Crafting, node: null, recipe: r, gather: null, cycles);
            OnChange?.Invoke();
            return true;
        }

        public bool StartSmelt(CraftRecipe r, int? cycles)
        {
            if (PlayerName is null) return false;
            if (!CanSmelt(r)) return false;
            if (cycles.HasValue && cycles.Value <= 0) cycles = null;

            if (!HasInputsForOne(r)) return false;

            _job = new ActiveJob(JobType.Smelting, node: null, recipe: r, gather: null, cycles);
            OnChange?.Invoke();
            return true;
        }

        public void StopChop() => StopJob();
        public void StopJob()
        {
            _job = null;
            OnChange?.Invoke();
        }

        public double GetAmount(string id)
            => _resources.TryGetValue(id, out var res) ? res.Amount : 0;

        public bool TryGetSellPrice(string id, out int price)
        {
            price = 0;
            if (ItemRegistry.TryGet(id, out var meta) && meta.SellPricePerUnit is int p)
            { price = p; return true; }
            return false;
        }

        public int Sell(string id, int qty)
        {
            qty = Math.Max(0, qty);
            if (qty == 0) return 0;

            if (!_resources.TryGetValue(id, out var res)) return 0;
            if (Math.Floor(res.Amount) < qty) qty = (int)Math.Floor(res.Amount);
            if (qty <= 0) return 0;

            if (!TryGetSellPrice(id, out var unit)) return 0;
            var earned = unit * qty;

            _resources[id] = res with { Amount = Math.Round(res.Amount - qty, 0) };
            var coins = _resources["coins"];
            _resources["coins"] = coins with { Amount = Math.Round(coins.Amount + earned, 0) };

            PushToast("🪙", $"+{earned} Coins");
            OnChange?.Invoke();
            return earned;
        }

        public bool HasInputsForOne(CraftRecipe r)
        {
            foreach (var i in r.Inputs)
            {
                var have = _resources.TryGetValue(i.ResourceId, out var res)
                    ? (int)Math.Floor(res.Amount)
                    : 0;
                if (have < i.Amount) return false;
            }
            return true;
        }

        private void Tick(TimeSpan dt)
        {
            DebugTicks++;
            // prune expired toasts
            _toasts.RemoveAll(t => t.ExpireAt <= DateTimeOffset.UtcNow);

            if (_job is null) { OnChange?.Invoke(); return; }
            // Generic gathering (woodcutting, mining)
            if (_job.Gather is not null)
            {
                var g = _job.Gather;
                _job.Advance(dt);
                if (!_job.Done) { OnChange?.Invoke(); return; }

                // roll integer yield
                var rng = Random.Shared;
                var amtDouble = g.Node.MinYield + (g.Node.MaxYield - g.Node.MinYield) * rng.NextDouble();
                var amt = Math.Max(1, (int)Math.Round(amtDouble));

                Add(g.OutputResourceId, amt);
                g.Skill.AddXp(g.Node.XpPerCycle);
                RecordMasteryProgress(MasteryIdForGatherNode(g.Node.Id), 1);
                GrantOverallXp(g.Node.XpPerCycle);

                var icon = g.OutputResourceId.Contains("ore") ? "⛏️" : "🌲";
                PushToast(icon, $"+{amt} {IdToNice(g.OutputResourceId)}");
                PushToast("⭐", $"+{g.Node.XpPerCycle:0} XP");

                _job.ResetForNextCycle();
                if (!_job.HasMoreCycles) _job = null;

                OnChange?.Invoke();
                return;
            }
            // Crafting
            if (_job.Type == JobType.Crafting && _job.Recipe is not null)
            {
                var r = _job.Recipe;

                if (_job.Elapsed == TimeSpan.Zero && !HasInputsForOne(r))
                { _job = null; OnChange?.Invoke(); return; }

                _job.Advance(dt);
                if (!_job.Done) { OnChange?.Invoke(); return; }

                // consume inputs
                foreach (var i in r.Inputs)
                {
                    var res = _resources[i.ResourceId];
                    _resources[i.ResourceId] = res with { Amount = Math.Max(0, Math.Round(res.Amount - i.Amount, 0)) };
                }
                // grant outputs
                foreach (var o in r.Outputs)
                {
                    var res = _resources[o.ResourceId];
                    _resources[o.ResourceId] = res with { Amount = Math.Round(res.Amount + o.Amount, 0) };
                    PushToast("🪵", $"+{o.Amount} {IdToNice(o.ResourceId)}");
                }
                Crafting.AddXp(r.XpPerCycle);
                GrantOverallXp(r.XpPerCycle);
                RecordMasteryProgress(MasteryIdForRecipe(r.Id), 1);
                PushToast("⭐", $"+{r.XpPerCycle:0} XP");

                _job.ResetForNextCycle();
                if (!_job.HasMoreCycles || !HasInputsForOne(r)) _job = null;

                OnChange?.Invoke();
                return;
            }
            // Smelting
            if (_job.Type == JobType.Smelting && _job.Recipe is not null)
            {
                var r = _job.Recipe;

                if (_job.Elapsed == TimeSpan.Zero && !HasInputsForOne(r))
                { _job = null; OnChange?.Invoke(); return; }

                _job.Advance(dt);
                if (!_job.Done) { OnChange?.Invoke(); return; }

                foreach (var i in r.Inputs)
                {
                    var res = _resources[i.ResourceId];
                    _resources[i.ResourceId] = res with { Amount = Math.Max(0, Math.Round(res.Amount - i.Amount, 0)) };
                }
                foreach (var o in r.Outputs)
                {
                    var res = _resources[o.ResourceId];
                    _resources[o.ResourceId] = res with { Amount = Math.Round(res.Amount + o.Amount, 0) };
                    PushToast("🔩", $"+{o.Amount} {IdToNice(o.ResourceId)}");
                }
                Smelting.AddXp(r.XpPerCycle);
                GrantOverallXp(r.XpPerCycle);
                RecordMasteryProgress(MasteryIdForRecipe(r.Id), 1);
                PushToast("⭐", $"+{r.XpPerCycle:0} XP");

                _job.ResetForNextCycle();
                if (!_job.HasMoreCycles || !HasInputsForOne(r)) _job = null;

                OnChange?.Invoke();
                return;
            }
            // Unknown job payload — clear
            _job = null;
            OnChange?.Invoke();
        }

        private void Add(string id, int amount)
        {
            if (!_resources.TryGetValue(id, out var r))
            {
                if (ItemRegistry.TryGet(id, out var meta))
                    r = new Resource(meta.Id, meta.Name, meta.Icon, 0);
                else
                    r = new Resource(id, IdToNice(id), "❓", 0);

                _resources[id] = r;
            }

            var newAmt = Math.Max(0, r.Amount + amount);
            _resources[id] = r with { Amount = newAmt };
        }

        private static string NiceUnitFor(string id)
        {
            if (id.Contains("ore")) return "ore";
            if (id.StartsWith("log_") || id == "wood") return "logs";
            if (id.Contains("bar")) return "bars";
            if (id.Contains("plank")) return "planks";
            return "units";
        }

        public static string IdToNice(string id)
        {
            return ItemRegistry.TryGet(id, out var meta)
                ? meta.Name
                : id.Replace('_', ' ');
        }

        private void PushToast(string icon, string text, double seconds = 3)
        {
            _toasts.Add(new Toast(icon, text, DateTimeOffset.UtcNow.AddSeconds(seconds)));
        }

        private void InitDefaults()
        {
            _resources = ItemRegistry.All.ToDictionary(
                m => m.Id,
                m => new Resource(m.Id, m.Name, m.Icon, 0)
            );
            _trees = TreeRegistry.All
                .Select(t => new WorkNode
                {
                    Id = t.Id,
                    Name = t.Name,
                    Icon = t.Icon,
                    RequiredLevel = t.RequiredLevel,
                    XpPerCycle = t.XpPerCycle,
                    MinYield = t.MinLogs,
                    MaxYield = t.MaxLogs,
                    Duration = TimeSpan.FromSeconds(t.DurationSeconds)
                })
                .ToList();

            _ores = OreRegistry.All
                .Select(o => new WorkNode
                {
                    Id = o.Id,
                    Name = o.Name,
                    Icon = o.Icon,
                    RequiredLevel = o.RequiredLevel,
                    XpPerCycle = o.XpPerCycle,
                    MinYield = o.YieldRange.min,
                    MaxYield = o.YieldRange.max,
                    Duration = TimeSpan.FromSeconds(o.DurationSeconds)
                })
                .ToList();
            _recipes = RecipeRegistry.Crafting.ToList();
            _smelt = RecipeRegistry.Smelting.ToList();
        }
        private PlayerData ToPlayerData()
        {
            var data = new PlayerData
            {
                Name = PlayerName ?? "Player",
                SaveVersion = CurrentSaveVersion,
                Resources = Resources.ToDictionary(kv => kv.Key, kv => kv.Value.Amount),
                Skills = this.Skills.ToSaveDictionary(),
                OverallLevel = this.OverallLevel,
                OverallXp = this.OverallXp,
                LastSavedUtc = DateTimeOffset.UtcNow,
                TotalClicks = _totalClicks,
            };

            if (_job is not null)
            {
                // persist node or recipe + elapsed
                data.ActiveJob = new PlayerData.ActiveJobSave
                {
                    NodeId = _job.Gather?.Node?.Id ?? _job.Node?.Id,
                    RecipeId = _job.Recipe?.Id,
                    ElapsedSeconds = Math.Clamp(_job.Elapsed.TotalSeconds, 0, _job.Duration.TotalSeconds)
                };
            }
            data.MinigameHighScores = _minigameHighScores;
            data.MinigameGamesPlayed = _minigameGamesPlayed;
            data.MasteryCounts = new(_masteryCounts);
            data.MasteryTiers = new(_masteryTiers);

            data.Equipment = _equipment
    .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
    .ToDictionary(
        kv => kv.Key.ToString(),
        kv => kv.Value!
    );

            return data;
        }
        private void ApplyPlayerData(PlayerData data)
        {
            PlayerName = string.IsNullOrWhiteSpace(data.Name) ? "Player" : data.Name;

            // Resources from save
            // Resources from save
            foreach (var kv in data.Resources)
            {
                var id = kv.Key;
                var amt = Math.Max(0, Math.Round(kv.Value, 0));

                // Prefer authoritative metadata from ItemRegistry
                if (ItemRegistry.TryGet(id, out var meta))
                {
                    _resources[id] = new Resource(meta.Id, meta.Name, meta.Icon, amt);
                }
                else if (_resources.TryGetValue(id, out var existing))
                {
                    // Keep existing if unknown in registry
                    _resources[id] = existing with { Amount = amt };
                }
                else
                {
                    // Fallback for unknown ids
                    _resources[id] = new Resource(id, IdToNice(id), "❓", amt);
                }
            }

            if (data.OverallLevel > 0) OverallLevel = data.OverallLevel;
            if (data.OverallXp >= 0) OverallXp = data.OverallXp;
            _totalClicks = Math.Max(0, data.TotalClicks);
            _minigameHighScores = data.MinigameHighScores ?? new(StringComparer.OrdinalIgnoreCase);
            _minigameGamesPlayed = Math.Max(0, data.MinigameGamesPlayed);
            _equipment.Keys.ToList().ForEach(k => _equipment[k] = null); // clear
            _masteryCounts.Clear();
            if (data.MasteryCounts != null)
                foreach (var kv in data.MasteryCounts) _masteryCounts[kv.Key] = Math.Max(0, kv.Value);

            _masteryTiers.Clear();
            if (data.MasteryTiers != null)
                foreach (var kv in data.MasteryTiers) _masteryTiers[kv.Key] = Math.Max(0, kv.Value);
            foreach (var kv in data.Equipment ?? new Dictionary<string, string>())
            {
                if (Enum.TryParse<GearSlot>(kv.Key, out var slot))
                    _equipment[slot] = kv.Value;
            }
            MigrateIfNeeded(data);
            // Ensure the known skills exist, then restore saved values
            Skills.EnsureKnownSkills([
                (SkillIds.Clicking,    "Clicking"),
                (SkillIds.Minigames,   "Minigames"),
                (SkillIds.Woodcutting, "Woodcutting"),
                (SkillIds.Crafting,    "Crafting"),
                (SkillIds.Mining,      "Mining"),
                (SkillIds.Smelting,    "Smelting"),
                (SkillIds.Casino,      "Casino"),
                (SkillIds.Mastery,     "Mastery"),
            ]);
            Skills.LoadFromDictionary(data.Skills);

            _job = null;
            if (data.ActiveJob is { } aj)
            {
                if (!string.IsNullOrEmpty(aj.NodeId))
                {
                    var node = _trees.FirstOrDefault(t => t.Id == aj.NodeId)
                            ?? _ores.FirstOrDefault(o => o.Id == aj.NodeId);

                    if (node is not null)
                    {
                        bool isWood = _trees.Any(t => t.Id == node.Id);
                        var outId = isWood ? TreeOutputId(node.Id) : node.Id;

                        var skill = isWood ? Woodcutting : Mining;
                        var type = isWood ? JobType.Woodcutting : JobType.Mining;

                        var spec = new GatherSpec($"{(isWood ? "woodcut" : "mining")}:{node.Id}", node, outId, skill);
                        _job = new ActiveJob(type, node, null, spec, cycles: null);
                        // honor saved elapsed time for gathering jobs
                        var elapsed = Math.Clamp(aj.ElapsedSeconds, 0, _job.Duration.TotalSeconds);
                        _job.Advance(TimeSpan.FromSeconds(elapsed));
                    }
                }
                else if (!string.IsNullOrEmpty(aj.RecipeId))
                {
                    var rec = _recipes.FirstOrDefault(r => r.Id == aj.RecipeId)
                           ?? _smelt.FirstOrDefault(r => r.Id == aj.RecipeId);

                    if (rec is not null)
                    {
                        var type = _recipes.Contains(rec) ? JobType.Crafting : JobType.Smelting;
                        _job = new ActiveJob(type, null, rec, gather: null, cycles: null);

                        var elapsed = Math.Clamp(aj.ElapsedSeconds, 0, _job.Duration.TotalSeconds);
                        _job.Advance(TimeSpan.FromSeconds(elapsed));
                    }
                }
            }
            OnChange?.Invoke();
        }
        public void RegisterClick()
        {
            _totalClicks++;
            Clicking.AddXp(1);         
            OnChange?.Invoke();
        }
        public async Task LoadFromLocalAsync()
        {
            if (_storage is null || _loadedFromLocal) { _loadedFromLocal = true; return; }

            string? json = null;
            try
            {
                json = await _storage.GetAsync(SaveKey); // calls storage.get
            }
            catch (Microsoft.JSInterop.JSException)
            {
                // JS not ready or storage.js missing; continue without a save
                _loadedFromLocal = true;
                OnChange?.Invoke();
                return;
            }
            if (string.IsNullOrWhiteSpace(json))
            {
                _loadedFromLocal = true;
                OnChange?.Invoke();
                return;
            }
            try
            {
                var data = System.Text.Json.JsonSerializer.Deserialize<PlayerData>(json, _json);
                if (data is not null) ApplyPlayerData(data);
            }
            catch { /* ignore corrupt save */ }
            finally
            {
                _loadedFromLocal = true;
                OnChange?.Invoke();
            }
        }
        public async Task SaveToLocalAsync(bool force = false)
        {
            if (_storage is null || !AutosaveEnabled) return;

            var now = DateTime.UtcNow;
            if (!force && (now - _lastSavedAt).TotalSeconds < 2) return; // debounce 2s
            _lastSavedAt = now;

            var data = ToPlayerData();
            var json = JsonSerializer.Serialize(data, _json);
            await _storage.SetAsync(SaveKey, json);
        }
        public async Task ClearLocalAsync()
        {
            if (_storage is null) return;
            await _storage.RemoveAsync(SaveKey);
        }
        public void GrantOverallXp(double amount)
        {
            if (amount <= 0) return;

            OverallXp += amount;

            // Level-up loop in case large amounts are granted at once
            while (OverallXp >= OverallXpNeededThisLevel)
            {
                OverallXp -= OverallXpNeededThisLevel;
                OverallLevel++;

                PushToast("⬆️", $"Overall Level {OverallLevel}");
            }
            OnChange?.Invoke();
        }
        public static double RequiredOverallXp(int level)
        {
            if (level < 1) level = 1;
            return 100 + 50 * (level - 1);
        }
        private void MigrateIfNeeded(PlayerData data)
        {
            if (data is null) return;

            // v0 → v1
            if (data.SaveVersion < 1)
            {
                // Ensure dictionaries exist
                data.Resources ??= new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                data.Skills ??= new Dictionary<string, PlayerData.SkillData>(StringComparer.OrdinalIgnoreCase);

                data.SaveVersion = 1; // mark migrated
            }
        }
        private static string TreeOutputId(string nodeId) => nodeId switch
        {
            "tree_1" => "log_t1",
            "tree_2" => "log_t2",
            "tree_3" => "log_t3",
            "tree_4" => "log_t4",
            "tree_5" => "log_t5",
            "tree_6" => "log_t6",
            "tree_7" => "log_t7",
            _ => "log_t1",
        };
        public bool ExchangeCoinsToChips(int amount)
        {
            amount = Math.Max(0, amount);
            if (amount == 0) return false;

            var coins = GetAmount("coins");
            if (coins < amount) return false;

            Add("coins", -amount);
            Add("chips", amount);

            PushToast("🎰", $"Exchanged {amount:0} Coins → {amount:0} Chips");
            OnChange?.Invoke();
            return true;
        }
        // Coinflip: bet `amount` Chips on heads/tails. Returns false if bet not placed.
        public bool TryCoinFlip(bool pickHeads, int amount, out bool win, out bool resultHeads)
        {
            win = false;
            resultHeads = false;

            amount = Math.Max(0, amount);
            if (amount <= 0) return false;

            var chips = GetAmount("chips");
            if (chips < amount) return false;

            Add("chips", -amount);

            // Flip with a small house edge (player wins slightly less than 50%)
            double roll = Random.Shared.NextDouble();
            double threshold = 0.5 - (CasinoHouseEdge / 2);
            resultHeads = roll < threshold;

            // Player wins only if their choice matches resultHeads
            win = (pickHeads == resultHeads);

            if (win)
            {
                Add("chips", amount * 2);
                PushToast("🪙", $"You won! {amount * 2:0} Chips paid. ({(resultHeads ? "Heads" : "Tails")})");
            }
            else
            {
                PushToast("🎲", $"You lost {amount:0} Chips. ({(resultHeads ? "Heads" : "Tails")})");
            }

            OnChange?.Invoke();
            return true;
        }
        public void SubmitMinigameScore(string gameId, double score)
        {
            if (string.IsNullOrWhiteSpace(gameId)) return;
            score = Math.Max(0, score);

            _minigameGamesPlayed++;

            var had = _minigameHighScores.TryGetValue(gameId, out var best);
            if (!had || score > best)
            {
                _minigameHighScores[gameId] = score;
                PushToast("🏆", $"New high score in {gameId}: {score:0}");
            }
            else
            {
                PushToast("🎮", $"Score: {score:0} (Best: {best:0})");
            }

            Minigames.AddXp(score);
            GrantOverallXp(Math.Max(1, score * 0.25));
            OnChange?.Invoke();
        }
        private readonly Dictionary<GearSlot, string?> _equipment = new()
        {
            [GearSlot.WoodTool] = null,
            [GearSlot.CraftTool] = null,
            [GearSlot.MiningTool] = null,
            [GearSlot.SmeltTool] = null,
            [GearSlot.Head] = null,
            [GearSlot.Chest] = null,
            [GearSlot.Legs] = null,
            [GearSlot.Feet] = null,
            [GearSlot.Hands] = null,
            [GearSlot.MainHand] = null,
            [GearSlot.OffHand] = null,
        };

        public IReadOnlyDictionary<GearSlot, string?> Equipment => _equipment;
        public string? GetEquipped(GearSlot slot) => _equipment.TryGetValue(slot, out var id) ? id : null;

        public void Equip(GearSlot slot, string itemId)
        {
            _equipment[slot] = itemId;
            PushToast("🧰", $"Equipped {IdToNice(itemId)} in {slot}");
            OnChange?.Invoke();
        }

        public void Unequip(GearSlot slot)
        {
            if (_equipment.ContainsKey(slot))
            {
                _equipment[slot] = null;
                PushToast("🧰", $"Unequipped {slot}");
                OnChange?.Invoke();
            }
        }
        // Casino XP anti-exploit accounting
        private readonly Queue<(DateTime, double)> _casinoXpWindow = new();
        private double _casinoXpAwardedToday = 0;
        private DateOnly _casinoXpDay = DateOnly.FromDateTime(DateTime.UtcNow);
        public void AwardCasinoXpFromBet(int betAmount, bool won)
        {
            if (betAmount <= 0) return;

            // 1) Sub-linear scaling (tunable)
            const double K = 1.4;
            double xpBase = K * Math.Sqrt(betAmount);
            if (won) xpBase *= 1.10; // tiny win bonus (optional)

            var now = DateTime.UtcNow;

            // 2) Rolling per-minute cap (tunable)
            const double PER_MIN_CAP = 50;

            // prune entries older than 60s
            while (_casinoXpWindow.Count > 0 && (now - _casinoXpWindow.Peek().Item1).TotalSeconds > 60)
                _casinoXpWindow.Dequeue();

            double windowSoFar = 0;
            foreach (var (_, xp) in _casinoXpWindow) windowSoFar += xp;

            double headroomWindow = Math.Max(0, PER_MIN_CAP - windowSoFar);
            if (headroomWindow <= 0) return;

            // 3) Daily cap (tunable & optionally scale with level)
            var today = DateOnly.FromDateTime(now);
            if (today != _casinoXpDay) { _casinoXpDay = today; _casinoXpAwardedToday = 0; }

            double dailyCap = 300 + 50 * Casino.Level; // adjust to taste
            double headroomDaily = Math.Max(0, dailyCap - _casinoXpAwardedToday);
            if (headroomDaily <= 0) return;

            // Final grant (respect both caps)
            double grant = Math.Min(xpBase, Math.Min(headroomWindow, headroomDaily));
            if (grant <= 0) return;

            // Record usage for caps
            _casinoXpWindow.Enqueue((now, grant));
            _casinoXpAwardedToday += grant;

            // Apply XP like your minigames pattern
            Casino.AddXp(grant);
            GrantOverallXp(Math.Max(1, grant * 0.25));
        }
        private static long TierTarget(int tier) => (long)Math.Pow(10, tier); // tier 1->10, 2->100, 3->1000…
        private static double MasteryProgressXpPerAction(long countBefore, int tierBefore) => 0.1; // tune
        private static double MasteryTierXpReward(int tier) => 10 + 10 * tier; // tune
        public void RecordMasteryProgress(string masteryId, int actions = 1)
        {
            if (actions <= 0) return;

            var before = _masteryCounts.TryGetValue(masteryId, out var cnt) ? cnt : 0;
            var newCount = before + actions;
            _masteryCounts[masteryId] = newCount;

            // Small drip XP per action
            var dripXp = MasteryProgressXpPerAction(before, _masteryTiers.TryGetValue(masteryId, out var tBefore) ? tBefore : 0) * actions;
            Mastery.AddXp(dripXp);
            GrantOverallXp(Math.Max(1, dripXp * 0.25));

            // Tier check(s) – handle multiple tiers if we crossed more than one target
            var currentTier = _masteryTiers.TryGetValue(masteryId, out var savedTier) ? savedTier : 0;
            var unlocked = 0;

            while (newCount >= TierTarget(currentTier + 1))
            {
                currentTier++;
                unlocked++;

                // XP reward for new tier
                var reward = MasteryTierXpReward(currentTier);
                Mastery.AddXp(reward);
                GrantOverallXp(Math.Max(1, reward * 0.25));

                // 🎟️ Token reward for new tier
                int tokenReward = (currentTier == 1) ? 2 : 1;
                Add(ResourceIds.MasteryToken, tokenReward);
                PushToast("🎟️", $"+{tokenReward} Mastery Token{(tokenReward > 1 ? "s" : "")}");

                // Announce tier unlock
                PushToast("🏆", $"Mastery Tier {currentTier} unlocked: {NiceMasteryId(masteryId)}");
            }

            if (unlocked > 0)
                _masteryTiers[masteryId] = currentTier;

            OnChange?.Invoke();
        }

        private static string NiceMasteryId(string id) =>
            id.Replace("gather:", "Gather ").Replace("craft:", "Craft ");
        // Public getters so UI can read mastery
        public long GetMasteryCount(string id) => _masteryCounts.TryGetValue(id, out var c) ? c : 0;
        public int GetMasteryTier(string id) => _masteryTiers.TryGetValue(id, out var t) ? t : 0;

        // Stable ids for mastery tracking
        public static string MasteryIdForGatherNode(string nodeId) => $"gather:{nodeId}";
        public static string MasteryIdForRecipe(string recipeId) => $"craft:{recipeId}";

        // Tier target curve (10, 100, 1,000, …)
        public static long MasteryTierTarget(int nextTier) => (long)Math.Pow(10, nextTier);
    }
}
