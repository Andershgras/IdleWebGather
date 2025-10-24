using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text.Json;
using IdleGatherWebGame.Models;

namespace IdleGatherWebGame.Services
{
    public sealed class GameState : IDisposable
    {
        // ---------- Public types used by UI ----------
        public sealed record Toast(string Icon, string Text, DateTimeOffset ExpireAt);
        // === Overall Player Level (runtime) ===
        public int OverallLevel { get; private set; } = 1;
        /** XP stored as "into this level" (not lifetime). */
        public double OverallXp { get; private set; } = 0;
        public ISkillService Skills { get; }

        public double OverallXpNeededThisLevel => RequiredOverallXp(OverallLevel);
        public double OverallXpToNextLevel => Math.Max(0, OverallXpNeededThisLevel - OverallXp);
        private const int CurrentSaveVersion = 1;
        public sealed class CraftRecipe
        {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public string Icon { get; set; } = "🛠️";
            public int RequiredLevel { get; set; }
            public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(4);
            public List<Input> Inputs { get; set; } = new();
            public List<Output> Outputs { get; set; } = new();
            public double XpPerCycle { get; set; } = 5;

            public sealed record Input(string ResourceId, int Amount);
            public sealed record Output(string ResourceId, int Amount);
        }

        // Tiny data for generic gathering
        private sealed record GatherSpec(
            string Id,               // e.g., "woodcut:tree_1" or "mining:ore_1"
            TreeNode Node,           // timing, min/max yield, xp
            string OutputResourceId, // resource granted per cycle
            Skill Skill              // which skill receives XP
        );

        private enum JobType { Woodcutting, Mining, Crafting, Smelting }

        // ---------- Save/load ----------
        private const string SaveKey = "idleGather.save.v1";
        private bool _loadedFromLocal = false;
        private DateTime _lastSavedAt = DateTime.MinValue;
        private static readonly JsonSerializerOptions _json = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly IBrowserStorage? _storage; // set via ctor with DI
        public bool AutosaveEnabled { get; set; } = true;
        private sealed class ActiveJob
        {
            public JobType Type { get; }
            public TreeNode? Node { get; }
            public CraftRecipe? Recipe { get; }
            public GatherSpec? Gather { get; } // non-null for gathering jobs
            public TimeSpan Duration { get; }
            public TimeSpan Elapsed { get; private set; }
            public int? CyclesRemaining { get; private set; } // null = infinite

            public ActiveJob(JobType type, TreeNode? node, CraftRecipe? recipe, GatherSpec? gather, int? cycles)
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

        // ---------- Public surface consumed by UI ----------
        public event Action? OnChange;

        public string? PlayerName { get; private set; }

        public IReadOnlyDictionary<string, Resource> Resources => _resources;
        public IReadOnlyList<TreeNode> TreeNodes => _trees;
        public IReadOnlyList<TreeNode> OreNodes => _ores;
        public IReadOnlyList<CraftRecipe> CraftingRecipes => _recipes;
        public IReadOnlyList<CraftRecipe> SmeltingRecipes => _smelt;

        public Skill Woodcutting => Skills.Get(SkillIds.Woodcutting);
        public Skill Crafting => Skills.Get(SkillIds.Crafting);
        public Skill Mining => Skills.Get(SkillIds.Mining);
        public Skill Smelting => Skills.Get(SkillIds.Smelting);
        public Skill Casino => Skills.Get(SkillIds.Casino);

        public bool JobRunning => _job is not null;
        public double Progress01 => _job?.Progress01 ?? 0;
        public double SecondsRemaining
            => _job is null ? 0 : Math.Max(0, (_job.Duration - _job.Elapsed).TotalSeconds);

        public int? ActiveCyclesRemaining => _job?.CyclesRemaining;
        public TreeNode? ActiveNode => _job?.Node;
        public CraftRecipe? ActiveRecipe => _job?.Recipe;

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
            { Gather: not null } j => $"{Math.Round(j.Gather!.Node.MinLogs)}–{Math.Round(j.Gather!.Node.MaxLogs)} " +
                                      $"{NiceUnitFor(j.Gather!.OutputResourceId)}, {Math.Round(j.Gather!.Node.XpPerCycle)} XP",
            { Recipe: not null } j => $"{string.Join(", ", j.Recipe!.Outputs.Select(o => $"{o.Amount} {IdToNice(o.ResourceId)}"))}, " +
                                      $"{Math.Round(j.Recipe!.XpPerCycle)} XP",
            _ => ""
        };

        public IReadOnlyList<Toast> Toasts => _toasts;
        public int DebugTicks { get; private set; }

        // ---------- Construction / Timer ----------
        public GameState(IBrowserStorage storage)
        {
            _storage = storage;

            // --- Initialize Skills (new) ---
            Skills = new SkillService();
            Skills.EnsureKnownSkills(new[]
            {
        (SkillIds.Woodcutting, "Woodcutting"),
        (SkillIds.Crafting,    "Crafting"),
        (SkillIds.Mining,      "Mining"),
        (SkillIds.Smelting,    "Smelting"),
        (SkillIds.Casino,      "Casino"),
    });

            InitDefaults();
            _timer = new Timer(_ => Tick(TimeSpan.FromMilliseconds(TickMs)), null, 250, TickMs);
        }
        public GameState() // fallback if DI ever calls parameterless
        {
            // --- Initialize Skills (new) ---
            Skills = new SkillService();
            Skills.EnsureKnownSkills(new[]
            {
        (SkillIds.Woodcutting, "Woodcutting"),
        (SkillIds.Crafting,    "Crafting"),
        (SkillIds.Mining,      "Mining"),
        (SkillIds.Smelting,    "Smelting"),
        (SkillIds.Casino,      "Casino"),
    });

            InitDefaults();
            _timer = new Timer(_ => Tick(TimeSpan.FromMilliseconds(TickMs)), null, 250, TickMs);
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        // ---------- Player ----------
        public void CreateCharacter(string name)
        {
            name = (name ?? "").Trim();
            if (string.IsNullOrEmpty(name)) name = "Player";
            if (name.Length > 16) name = name.Substring(0, 16);
            PlayerName = name;
            OnChange?.Invoke();
        }

        // ---------- Public API: Start/Stop ----------
        public bool CanChop(TreeNode n) => Woodcutting.Level >= n.RequiredLevel;
        public bool CanMine(TreeNode n) => Mining.Level >= n.RequiredLevel;
        public bool CanCraft(CraftRecipe r) => Crafting.Level >= r.RequiredLevel;
        public bool CanSmelt(CraftRecipe r) => Smelting.Level >= r.RequiredLevel;

        public bool StartChop(TreeNode node, int? cycles)
        {
            if (PlayerName is null) return false;
            if (!CanChop(node)) return false;

            if (!_treeOutput.TryGetValue(node.Id, out var woodOutId))
                woodOutId = "log_t1"; // fallback for unknown nodes

            var spec = new GatherSpec($"woodcut:{node.Id}", node, woodOutId, Woodcutting);

            _job = new ActiveJob(JobType.Woodcutting, node, recipe: null, gather: spec, cycles);
            OnChange?.Invoke();
            return true;
        }

        public bool StartMine(TreeNode node, int? cycles)
        {
            if (PlayerName is null) return false;
            if (!CanMine(node)) return false;
            if (!_nodeOutput.TryGetValue(node.Id, out var outId)) return false;

            var spec = new GatherSpec($"mining:{node.Id}", node, outId, Mining);
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

        // ---------- Inventory helpers ----------
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

        // ---------- Tick / job engine ----------
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
                var amtDouble = g.Node.MinLogs + (g.Node.MaxLogs - g.Node.MinLogs) * rng.NextDouble();
                var amt = Math.Max(1, (int)Math.Round(amtDouble));

                Add(g.OutputResourceId, amt);
                g.Skill.AddXp(g.Node.XpPerCycle);
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

        // ---------- Private helpers ----------
        private void Add(string id, int amount)
        {
            if (!_resources.TryGetValue(id, out var res))
            {
                // fallback entry to avoid crashes if a new item wasn't registered
                _resources[id] = new Resource(id, IdToNice(id), "❓", 0);
                res = _resources[id];
            }

            _resources[id] = res with { Amount = Math.Max(0, Math.Round(res.Amount + amount, 0)) };
        }

        private static string NiceUnitFor(string id)
        {
            if (id.Contains("ore")) return "ore";
            if (id.StartsWith("log_") || id == "wood") return "logs"; // CHANGED
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

        // ---------- Content / data ----------
        private void InitDefaults()
        {
            // Build resources from ItemRegistry (names/icons centralized)
            _resources = ItemRegistry.All.ToDictionary(
                m => m.Id,
                m => new Resource(m.Id, m.Name, m.Icon, 0)
            );
            // Woodcutting: build trees from TreeRegistry
            _trees = TreeRegistry.All
                .Select(t => new TreeNode
                {
                    Id = t.Id,
                    Name = t.Name,
                    Icon = t.Icon,
                    RequiredLevel = t.RequiredLevel,
                    XpPerCycle = t.XpPerCycle,
                    MinLogs = t.MinLogs,
                    MaxLogs = t.MaxLogs,
                    Duration = TimeSpan.FromSeconds(t.DurationSeconds)
                })
                .ToList();

            // Mining (ore) nodes (reuse TreeNode)
            _ores = new List<TreeNode>
            {
                new TreeNode{ Id="ore_1", Name="Copper Vein", Icon="⛏️", RequiredLevel=1,  MinLogs=1, MaxLogs=3, XpPerCycle=6,  Duration=TimeSpan.FromSeconds(6)},
                new TreeNode{ Id="ore_2", Name="Tin Vein",    Icon="⛏️", RequiredLevel=3,  MinLogs=1, MaxLogs=3, XpPerCycle=7,  Duration=TimeSpan.FromSeconds(6)},
                new TreeNode{ Id="ore_3", Name="Iron Vein",   Icon="⛏️", RequiredLevel=8,  MinLogs=1, MaxLogs=2, XpPerCycle=10, Duration=TimeSpan.FromSeconds(6)},
                new TreeNode{ Id="ore_4", Name="Silver Vein", Icon="⛏️", RequiredLevel=15, MinLogs=1, MaxLogs=2, XpPerCycle=14, Duration=TimeSpan.FromSeconds(6)},
                new TreeNode{ Id="ore_5", Name="Gold Vein",   Icon="⛏️", RequiredLevel=20, MinLogs=1, MaxLogs=2, XpPerCycle=18, Duration=TimeSpan.FromSeconds(6)},
                new TreeNode{ Id="ore_6", Name="Mithril",     Icon="⛏️", RequiredLevel=30, MinLogs=1, MaxLogs=2, XpPerCycle=24, Duration=TimeSpan.FromSeconds(6)},
                new TreeNode{ Id="ore_7", Name="Adamantite",  Icon="⛏️", RequiredLevel=40, MinLogs=1, MaxLogs=2, XpPerCycle=30, Duration=TimeSpan.FromSeconds(6)},
            };

            // map mining node -> output resource id
            _nodeOutput = new Dictionary<string, string>
            {
                ["ore_1"] = "copper_ore",
                ["ore_2"] = "tin_ore",
                ["ore_3"] = "iron_ore",
                ["ore_4"] = "silver_ore",
                ["ore_5"] = "gold_ore",
                ["ore_6"] = "mithril_ore",
                ["ore_7"] = "adamant_ore",
            };
            // map woodcutting node -> output resource id (NEW)
            _treeOutput = new Dictionary<string, string>
            {
                ["tree_1"] = "log_t1", // Shrub
                ["tree_2"] = "log_t2", // Oak
                ["tree_3"] = "log_t3", // Willow
                ["tree_4"] = "log_t4", // Maple
                ["tree_5"] = "log_t5", // Yew
                ["tree_6"] = "log_t6", // Magic
                ["tree_7"] = "log_t7", // Elder
            };

            // Crafting recipes (planks T1..T7 as example)
            _recipes = new List<CraftRecipe>
            {
                new CraftRecipe{
                    Id="plank_t1", Name="Plank (T1)", Icon="🪵",
                    RequiredLevel=1, Duration=TimeSpan.FromSeconds(4),
                    Inputs=new(){ new("log_t1", 2) },
                    Outputs=new(){ new("plank_t1",1) }, XpPerCycle=6
                },
                new CraftRecipe{
                Id="plank_t2", Name="Plank (T2)", Icon="🪵",
                RequiredLevel=5, Duration=TimeSpan.FromSeconds(5),
                Inputs=new(){ new("log_t2", 2) },  // NEW
                Outputs=new(){ new("plank_t2",1) }, XpPerCycle=10
                },
            };

            // Smelting recipes
            _smelt = new List<CraftRecipe>
            {
                new CraftRecipe{ Id="bronze_bar", Name="Bronze Bar", Icon="🔩", RequiredLevel=1,  Duration=TimeSpan.FromSeconds(4),
                    Inputs=new(){ new("copper_ore",1), new("tin_ore",1) }, Outputs=new(){ new("bronze_bar",1) }, XpPerCycle=8 },
                new CraftRecipe{ Id="iron_bar",   Name="Iron Bar",   Icon="🔩", RequiredLevel=8,  Duration=TimeSpan.FromSeconds(4),
                    Inputs=new(){ new("iron_ore",2) }, Outputs=new(){ new("iron_bar",1) }, XpPerCycle=12 },
                new CraftRecipe{ Id="silver_bar", Name="Silver Bar", Icon="🔩", RequiredLevel=15, Duration=TimeSpan.FromSeconds(4),
                    Inputs=new(){ new("silver_ore",2) }, Outputs=new(){ new("silver_bar",1) }, XpPerCycle=16 },
                new CraftRecipe{ Id="gold_bar",   Name="Gold Bar",   Icon="🔩", RequiredLevel=20, Duration=TimeSpan.FromSeconds(4),
                    Inputs=new(){ new("gold_ore",2) }, Outputs=new(){ new("gold_bar",1) }, XpPerCycle=20 },
                new CraftRecipe{ Id="mith_bar",   Name="Mithril Bar",Icon="🔩", RequiredLevel=30, Duration=TimeSpan.FromSeconds(4),
                    Inputs=new(){ new("mithril_ore",3) }, Outputs=new(){ new("mith_bar",1) }, XpPerCycle=28 },
                new CraftRecipe{ Id="adam_bar",   Name="Adamant Bar",Icon="🔩", RequiredLevel=40, Duration=TimeSpan.FromSeconds(4),
                    Inputs=new(){ new("adamant_ore",3) }, Outputs=new(){ new("adam_bar",1) }, XpPerCycle=34 },
            };
        }

        // ---------- Fields ----------
        private const int TickMs = 200; // 5 ticks/sec for smoother bars
        private readonly List<Toast> _toasts = new();
        private Dictionary<string, Resource> _resources = new();
        private List<TreeNode> _trees = new();
        private List<TreeNode> _ores = new();
        private List<CraftRecipe> _recipes = new();
        private List<CraftRecipe> _smelt = new();
        private Dictionary<string, string> _nodeOutput = new();
        private Dictionary<string, string> _treeOutput = new();

        private ActiveJob? _job;
        private readonly Timer _timer;

        // ---------- Save/load implementation ----------
        private PlayerData ToPlayerData()
        {
            var data = new PlayerData
            {
                Name = PlayerName ?? "Player",
                SaveVersion = CurrentSaveVersion,
                // resources and skills
                Resources = Resources.ToDictionary(kv => kv.Key, kv => kv.Value.Amount),
                Skills = this.Skills.ToSaveDictionary(),

                // NEW: overall progression (persist runtime values)
                OverallLevel = this.OverallLevel,
                OverallXp = this.OverallXp,

                // timestamp
                LastSavedUtc = DateTimeOffset.UtcNow
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

            return data;
        }
        private void ApplyPlayerData(PlayerData data)
        {
            PlayerName = string.IsNullOrWhiteSpace(data.Name) ? "Player" : data.Name;

            // --- Resources from save ---
            foreach (var kv in data.Resources)
            {
                double amt = Math.Max(0, Math.Round(kv.Value, 0));
                if (_resources.TryGetValue(kv.Key, out var r))
                {
                    _resources[kv.Key] = r with { Amount = amt };
                }
                else
                {
                    _resources[kv.Key] = new Resource(kv.Key, IdToNice(kv.Key), "❓", amt);
                }
            }

            // --- Overall progression ---
            if (data.OverallLevel > 0) OverallLevel = data.OverallLevel;
            if (data.OverallXp >= 0) OverallXp = data.OverallXp;

            MigrateIfNeeded(data);
            // Ensure the known skills exist, then restore saved values
            Skills.EnsureKnownSkills(new[]
            {
                (SkillIds.Woodcutting, "Woodcutting"),
                (SkillIds.Crafting,    "Crafting"),
                (SkillIds.Mining,      "Mining"),
                (SkillIds.Smelting,    "Smelting"),
                (SkillIds.Casino,      "Casino"),
            });
            Skills.LoadFromDictionary(data.Skills);

            // --- Active job (rebuild) ---
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

                        string outId;
                        if (isWood)
                        {
                            // Use new per-tree wood output (e.g., tree_1 -> log_t1, tree_2 -> log_t2, ...)
                            outId = _treeOutput.TryGetValue(node.Id, out var wId) ? wId : "log_t1";
                        }
                        else
                        {
                            // Mining still uses ore output map
                            outId = _nodeOutput.TryGetValue(node.Id, out var rId) ? rId : "stone";
                        }

                        var skill = isWood ? Woodcutting : Mining;
                        var type = isWood ? JobType.Woodcutting : JobType.Mining;

                        var spec = new GatherSpec($"{(isWood ? "woodcut" : "mining")}:{node.Id}", node, outId, skill);
                        _job = new ActiveJob(type, node, null, spec, cycles: null);

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

            // --- One-time migration: legacy "wood" -> "log_t1" ---
            if (_resources.TryGetValue("wood", out var legacy) && legacy.Amount > 0)
            {
                var amt = legacy.Amount;

                if (_resources.TryGetValue("log_t1", out var t1))
                    _resources["log_t1"] = t1 with { Amount = t1.Amount + amt };
                else
                    _resources["log_t1"] = new Resource("log_t1", "Log (T1)", "🌲", amt);

                _resources["wood"] = legacy with { Amount = 0 };
                PushToast("🪵", $"Migrated {amt:0} Wood → Log (T1)");
            }

            OnChange?.Invoke();
        }

        public async Task LoadFromLocalAsync()
        {
            if (_storage is null || _loadedFromLocal) { _loadedFromLocal = true; return; }

            string? json = null;
            try
            {
                json = await _storage.GetAsync(SaveKey);  // calls storage.get
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
        // Overall XP methods
        public void GrantOverallXp(double amount)
        {
            if (amount <= 0) return;

            OverallXp += amount;

            // Level-up loop in case large amounts are granted at once
            while (OverallXp >= OverallXpNeededThisLevel)
            {
                OverallXp -= OverallXpNeededThisLevel;
                OverallLevel++;

                // Optional: player feedback
                PushToast("⬆️", $"Overall Level {OverallLevel}");
            }
            OnChange?.Invoke();
        }
        public static double RequiredOverallXp(int level)
        {
            if (level < 1) level = 1;
            // Simple, tweakable curve:
            // 100 + 50*(L-1)  → L1:100, L2:150, L3:200, ...
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

                // Example of safe ID rename (ONLY if you actually renamed IDs):
                // if (data.Resources.Remove("wood", out var amt))
                // {
                //     if (data.Resources.ContainsKey("log_t1")) data.Resources["log_t1"] += amt;
                //     else data.Resources["log_t1"] = amt;
                // }

                data.SaveVersion = 1; // mark migrated
            }
        }
    }
}
