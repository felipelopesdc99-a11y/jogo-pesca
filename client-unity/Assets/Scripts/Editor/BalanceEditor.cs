using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FishingIdle.Game.Bootstrap;
using FishingIdle.GameService.Config;
using FishingIdle.Texts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using T = FishingIdle.Texts.GameTexts.DevPanel;

namespace FishingIdle.Editor
{
    /// <summary>
    /// The Balanceamento tab: edits the files in /config without touching code.
    /// </summary>
    /// <remarks>
    /// Files are loaded as JSON trees and edited in place, so only the values the owner changes
    /// differ after a save — key order, notes and formatting stay as they were. Nothing is written
    /// unless the complete set passes the game's own validation (GameConfigLoader), the same check
    /// the game runs on start.
    /// </remarks>
    public sealed class BalanceEditor
    {
        // Always the same list the game loads, so the panel can never validate against a partial set.
        private static readonly IReadOnlyList<string> Files = GameConfigLoader.RequiredFiles;

        private readonly Dictionary<string, JObject> _documents = new Dictionary<string, JObject>();
        private readonly Dictionary<string, string> _originals = new Dictionary<string, string>();
        private readonly HashSet<string> _dirty = new HashSet<string>();
        private readonly List<string> _messages = new List<string>();
        private string _loadError;
        private MessageType _messageType = MessageType.Info;
        private int _section;
        private Vector2 _scroll;

        public bool HasUnsavedChanges => _dirty.Count > 0;

        public void Load()
        {
            _documents.Clear();
            _originals.Clear();
            _dirty.Clear();
            _loadError = null;

            foreach (var file in Files)
            {
                var path = Path.Combine(GamePaths.RepositoryConfigDirectory, file);
                try
                {
                    var text = File.ReadAllText(path, Encoding.UTF8);
                    _originals[file] = text;
                    _documents[file] = JObject.Parse(text);
                }
                catch (Exception exception)
                {
                    _loadError = GameTexts.Validation.ConfigFileInvalidJson(file, exception.Message);
                }
            }
        }

        public void Draw()
        {
            if (_documents.Count == 0 && _loadError == null)
            {
                Load();
            }

            EditorGUILayout.HelpBox(T.BalanceIntro, MessageType.None);
            if (_loadError != null)
            {
                EditorGUILayout.HelpBox(_loadError, MessageType.Error);
                return;
            }

            DrawActions();

            _section = GUILayout.Toolbar(_section, new[] { T.SectionFishing, T.SectionSpecies, T.SectionMaps, T.SectionSizes, T.SectionRarities, T.SectionXp, T.SectionRods, T.SectionEconomy, T.SectionOthers });
            EditorGUILayout.Space(6);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            switch (_section)
            {
                case 0: DrawFishing(); break;
                case 1: DrawSpecies(); break;
                case 2: DrawMaps(); break;
                case 3: DrawSizes(); break;
                case 4: DrawRarities(); break;
                case 5: DrawXp(); break;
                case 6: DrawRods(); break;
                case 7: DrawEconomy(); break;
                default: DrawOthers(); break;
            }

            EditorGUILayout.EndScrollView();
        }

        // ------------------------------------------------------------------ actions

        private void DrawActions()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = HasUnsavedChanges;
                if (GUILayout.Button(EditorApplication.isPlaying ? T.SaveAndApply : T.Save, GUILayout.Height(26)))
                {
                    Save();
                }

                if (GUILayout.Button(T.Discard, GUILayout.Height(26)))
                {
                    Load();
                    _messages.Clear();
                }

                GUI.enabled = true;
                if (GUILayout.Button(T.Reload, GUILayout.Width(110), GUILayout.Height(26)))
                {
                    Load();
                    _messages.Clear();
                }
            }

            if (HasUnsavedChanges)
            {
                EditorGUILayout.HelpBox(T.Unsaved + " (" + string.Join(", ", _dirty) + ")", MessageType.Warning);
            }

            if (_messages.Count > 0)
            {
                EditorGUILayout.HelpBox(string.Join("\n", _messages), _messageType);
            }
        }

        private void Save()
        {
            _messages.Clear();
            var texts = Files.ToDictionary(f => f, f => _dirty.Contains(f) ? Serialize(_documents[f]) : _originals[f]);

            var check = GameConfigLoader.LoadFromTexts(texts);
            var errors = new List<string>(check.Errors);
            errors.AddRange(ValidateOthers());
            if (errors.Count > 0)
            {
                _messageType = MessageType.Error;
                _messages.Add(T.InvalidNotSaved);
                _messages.AddRange(errors.Select(e => "• " + e));
                return;
            }

            foreach (var file in _dirty)
            {
                File.WriteAllText(Path.Combine(GamePaths.RepositoryConfigDirectory, file), texts[file], new UTF8Encoding(false));
                _originals[file] = texts[file];
            }

            _dirty.Clear();
            _messageType = MessageType.Info;

            if (EditorApplication.isPlaying && GameRoot.Instance != null)
            {
                var reloadErrors = GameRoot.Instance.ReloadConfig();
                if (reloadErrors.Count > 0)
                {
                    _messageType = MessageType.Error;
                    _messages.AddRange(reloadErrors);
                    return;
                }

                _messages.Add(T.SavedAndApplied);
            }
            else
            {
                _messages.Add(T.Saved);
            }
        }

        /// <summary>Extra safety checks on top of the game's validation: no negative numbers anywhere in these files.</summary>
        private IEnumerable<string> ValidateOthers()
        {
            foreach (var file in new[] { GameConfigLoader.ArenaFile, GameConfigLoader.ExpeditionsFile, GameConfigLoader.EconomyFile, GameConfigLoader.ArenaBotsFile })
            {
                foreach (var token in _documents[file].Descendants().OfType<JValue>())
                {
                    if ((token.Type == JTokenType.Integer || token.Type == JTokenType.Float) && token.ToObject<double>() < 0)
                    {
                        yield return GameTexts.Validation.NegativeValue(file, token.Path);
                    }
                }
            }

            var roll = _documents["arena.json"].SelectToken("combat.damage_roll");
            if (roll != null && roll.Value<double>("min") > roll.Value<double>("max"))
            {
                yield return GameTexts.Validation.BadIntRange("arena.json", "combat.damage_roll");
            }
        }

        private static string Serialize(JObject document)
        {
            // Same shape as the files in the repository: two-space indent, LF, final newline.
            return document.ToString(Formatting.Indented).Replace("\r\n", "\n") + "\n";
        }

        // ------------------------------------------------------------------ sections

        private void DrawFishing()
        {
            const string file = GameConfigLoader.ProgressionFile;
            Number(file, "fishing.online_cycle_seconds", T.OnlineCycle, 1);
            EditorGUILayout.Space(8);
            Number(file, "fishing.offline_cycle_seconds", T.OfflineCycle, 1);
            Number(file, "fishing.offline_accumulation_cap_hours", T.OfflineCap, 0);
            EditorGUILayout.HelpBox(T.OfflineNote, MessageType.None);
        }

        private void DrawSpecies()
        {
            const string file = GameConfigLoader.FishCatalogFile;
            var species = (JArray)_documents[file]["species"];
            if (species == null)
            {
                return;
            }

            Header(new[] { T.SpeciesName, T.SpeciesRarity, T.SpeciesSizeMin, T.SpeciesSizeMax, T.SpeciesSale, T.SpeciesFisherXp, T.SpeciesFeedXp, T.StatHp, T.StatAttack, T.StatDefense, T.StatSpeed });
            for (var i = 0; i < species.Count; i++)
            {
                var prefix = "species[" + i + "].";
                using (new EditorGUILayout.HorizontalScope())
                {
                    Text(file, prefix + "display_name", 130);
                    GUILayout.Label(RarityName(species[i].Value<string>("rarity")), GUILayout.Width(70));
                    Cell(file, prefix + "size_cm.min");
                    Cell(file, prefix + "size_cm.max");
                    Cell(file, prefix + "base_sale_value_coins");
                    Cell(file, prefix + "base_fisher_xp");
                    Cell(file, prefix + "base_feed_xp");
                    Cell(file, prefix + "base_stats.hp");
                    Cell(file, prefix + "base_stats.attack");
                    Cell(file, prefix + "base_stats.defense");
                    Cell(file, prefix + "base_stats.speed");
                }
            }

            EditorGUILayout.HelpBox(T.StatsNote, MessageType.None);
        }

        private void DrawMaps()
        {
            const string file = GameConfigLoader.MapsFile;
            var maps = (JArray)_documents[file]["maps"];
            if (maps == null)
            {
                return;
            }

            Number(file, "travel.duration_seconds", T.TravelSeconds, 0);
            EditorGUILayout.Space(6);

            for (var m = 0; m < maps.Count; m++)
            {
                var map = maps[m];
                EditorGUILayout.LabelField(map.Value<string>("display_name"), EditorStyles.boldLabel);
                Number(file, "maps[" + m + "].unlock_fisher_level", T.UnlockLevel, 1);

                var pool = (JArray)map["fish_pool"];
                var total = pool.Sum(e => Math.Max(0, e.Value<double>("catch_weight")));
                Header(new[] { T.SpeciesName, T.CatchWeight, T.CatchChance });
                for (var e = 0; e < pool.Count; e++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label(SpeciesName(pool[e].Value<string>("species_id")), GUILayout.Width(130));
                        Cell(file, "maps[" + m + "].fish_pool[" + e + "].catch_weight");
                        var weight = Math.Max(0, pool[e].Value<double>("catch_weight"));
                        GUILayout.Label(total > 0 ? Format.Percent(weight / total, 1) : "—", GUILayout.Width(70));
                    }
                }

                EditorGUILayout.Space(10);
            }
        }

        private void DrawSizes()
        {
            const string file = GameConfigLoader.ProgressionFile;
            var categories = (JArray)_documents[file].SelectToken("size.categories");
            if (categories == null)
            {
                return;
            }

            var total = categories.Sum(c => Math.Max(0, c.Value<double>("draw_weight")));
            Header(new[] { T.SizeCategory, T.SizeWeight, T.CatchChance, T.SizePercentileMin, T.SizePercentileMax, T.SizeXpMultiplier });
            for (var i = 0; i < categories.Count; i++)
            {
                var prefix = "size.categories[" + i + "].";
                using (new EditorGUILayout.HorizontalScope())
                {
                    Text(file, prefix + "display_name", 130);
                    Cell(file, prefix + "draw_weight");
                    var weight = Math.Max(0, categories[i].Value<double>("draw_weight"));
                    GUILayout.Label(total > 0 ? Format.Percent(weight / total, 1) : "—", GUILayout.Width(70));
                    Cell(file, prefix + "percentile_min");
                    Cell(file, prefix + "percentile_max");
                    Cell(file, prefix + "fisher_xp_multiplier");
                }
            }

            EditorGUILayout.Space(8);
            Number(file, "size.sale_value_influence.influence", T.SaleInfluence, 0);
            Number(file, "size.stat_influence.influence", T.StatInfluence, 0);
            Number(file, "size.feed_xp_influence.influence", T.FeedInfluence, 0);
        }

        /// <summary>
        /// What each rarity is worth over the species' base values, and how it changes the size draw
        /// (size_weight_multipliers, addendum A-083), with the resulting chance of each size.
        /// </summary>
        private void DrawRarities()
        {
            const string file = GameConfigLoader.ProgressionFile;
            var tiers = (JArray)_documents[file].SelectToken("rarity.tiers");
            var categories = (JArray)_documents[file].SelectToken("size.categories");
            if (tiers == null || categories == null)
            {
                return;
            }

            EditorGUILayout.LabelField(T.RarityValuesTitle, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(T.RarityValuesNote, MessageType.None);
            Header(new[] { T.RarityName, T.RaritySale, T.RarityStat, T.RarityFisherXp, T.RarityFeedXp });
            for (var i = 0; i < tiers.Count; i++)
            {
                var prefix = "rarity.tiers[" + i + "].";
                using (new EditorGUILayout.HorizontalScope())
                {
                    Text(file, prefix + "display_name", 130);
                    Cell(file, prefix + "sale_value_multiplier");
                    Cell(file, prefix + "stat_multiplier");
                    Cell(file, prefix + "fisher_xp_multiplier");
                    Cell(file, prefix + "feed_xp_multiplier");
                }
            }

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField(T.RaritySizeTitle, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(T.RaritySizeNote, MessageType.None);
            var names = categories.Select(c => c.Value<string>("display_name") ?? c.Value<string>("id")).ToList();
            Header(new[] { T.RarityName }.Concat(names.Select(T.TimesSize)).Concat(names));
            foreach (JObject tier in tiers)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(tier.Value<string>("display_name") ?? tier.Value<string>("id"), GUILayout.Width(130));
                    var weights = new List<double>();
                    foreach (var category in categories)
                    {
                        var id = category.Value<string>("id");
                        var multiplier = SizeMultiplierCell(file, tier, id);
                        weights.Add(Math.Max(0, category.Value<double>("draw_weight")) * multiplier);
                    }

                    var total = weights.Sum();
                    foreach (var w in weights)
                    {
                        GUILayout.Label(total > 0 ? Format.Percent(w / total, w / total < 0.01 ? 2 : 1) : "—", GUILayout.Width(70));
                    }
                }
            }
        }

        /// <summary>
        /// One size multiplier of a rarity. A size not listed counts as 1 and is only written to the
        /// file when the owner changes it, so untouched rarities keep their file unchanged.
        /// </summary>
        private double SizeMultiplierCell(string file, JObject tier, string sizeId)
        {
            var map = tier["size_weight_multipliers"] as JObject;
            var current = map?[sizeId] is JValue v ? v.Value<double>() : 1.0;
            EditorGUI.BeginChangeCheck();
            var result = Math.Max(0.0, EditorGUILayout.DoubleField(current, GUILayout.Width(70)));
            if (EditorGUI.EndChangeCheck())
            {
                if (map == null)
                {
                    map = new JObject();
                    tier["size_weight_multipliers"] = map;
                }

                map[sizeId] = result;
                _dirty.Add(file);
                return result;
            }

            return current;
        }

        private void DrawXp()
        {
            const string file = GameConfigLoader.ProgressionFile;
            var table = (JArray)_documents[file].SelectToken("fisher.xp_table");
            if (table == null)
            {
                return;
            }

            long total = 0;
            for (var i = 0; i < Math.Min(9, table.Count); i++)
            {
                total += table[i].Value<long>("xp_to_next_level");
            }

            EditorGUILayout.LabelField(T.XpTotalTo10, Format.Number(total));
            Number(file, "fish_level.stat_bonus_per_level_percent", T.FishLevelBonus, 0);
            Number(file, "feeding.invested_xp_recovery_ratio", T.FeedRecovery, 0);
            EditorGUILayout.Space(6);

            var fishTable = (JArray)_documents[file].SelectToken("fish_level.xp_table");
            if (fishTable != null)
            {
                EditorGUILayout.LabelField(T.FishXpTable, EditorStyles.boldLabel);
                Header(new[] { T.XpLevel, T.XpToNext });
                for (var i = 0; i < fishTable.Count; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label(fishTable[i].Value<int>("level").ToString(), GUILayout.Width(130));
                        Cell(file, "fish_level.xp_table[" + i + "].xp_to_next_level");
                    }
                }

                EditorGUILayout.Space(10);
            }

            EditorGUILayout.LabelField(T.FisherXpTable, EditorStyles.boldLabel);
            Header(new[] { T.XpLevel, T.XpToNext });
            for (var i = 0; i < table.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(table[i].Value<int>("level").ToString(), GUILayout.Width(130));
                    Cell(file, "fisher.xp_table[" + i + "].xp_to_next_level");
                }
            }
        }

        private void DrawRods()
        {
            const string file = GameConfigLoader.RodsFile;
            var rods = (JArray)_documents[file]["rods"];
            if (rods == null)
            {
                return;
            }

            for (var r = 0; r < rods.Count; r++)
            {
                var rod = rods[r];
                var prefix = "rods[" + r + "].";
                EditorGUILayout.LabelField(rod.Value<string>("display_name"), EditorStyles.boldLabel);
                Number(file, prefix + "acquisition.purchase_cost_coins", T.RodPrice, 0);
                Number(file, prefix + "acquisition.unlock_fisher_level", T.UnlockLevel, 1);

                if (rod["bonuses_per_level"] != null)
                {
                    Header(new[] { T.XpLevel, T.RodRarityBonus, T.RodSizeBonus, T.RodShellBonus, T.RodUpgradeCost });
                    var levels = ((JArray)rod.SelectToken("bonuses_per_level.rarity_efficiency")).Count;
                    for (var level = 0; level < levels; level++)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Label((level + 1).ToString(), GUILayout.Width(130));
                            Cell(file, prefix + "bonuses_per_level.rarity_efficiency[" + level + "]");
                            Cell(file, prefix + "bonuses_per_level.size_quality[" + level + "]");
                            Cell(file, prefix + "bonuses_per_level.shell_yield[" + level + "]");
                            if (level > 0)
                            {
                                Cell(file, prefix + "upgrade_costs[" + (level - 1) + "].cost_coins");
                            }
                        }
                    }
                }

                EditorGUILayout.Space(10);
            }

            EditorGUILayout.HelpBox(T.RodsNote, MessageType.None);
        }

        private void DrawEconomy()
        {
            const string file = GameConfigLoader.EconomyFile;
            Number(file, "aquarium.hard_capacity", T.AquariumCapacity, 1);
            Number(file, "npc_fish_sale.minimum_price_coins", T.MinimumPrice, 0);
            Number(file, "shells.base_drop_chance_per_catch", T.ShellChance, 0);
            Number(file, "shells.amount_per_drop.min", T.ShellMin, 0);
            Number(file, "shells.amount_per_drop.max", T.ShellMax, 0);

            DrawFieldGroups(GameTexts.DevPanel.EconomyFields);
        }

        private void DrawOthers()
        {
            EditorGUILayout.HelpBox(T.OthersNote, MessageType.None);
            DrawFieldGroups(GameTexts.DevPanel.OtherFields);

            const string file = "expeditions.json";
            var expeditions = (JArray)_documents[file]["expeditions"];
            if (expeditions == null)
            {
                return;
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField(T.ExpeditionsTable, EditorStyles.boldLabel);
            Header(new[] { T.SpeciesName, T.ExpDuration, T.ExpStrength, T.ExpCoins, T.ExpFishChance });
            for (var i = 0; i < expeditions.Count; i++)
            {
                var prefix = "expeditions[" + i + "].";
                using (new EditorGUILayout.HorizontalScope())
                {
                    Text(file, prefix + "display_name", 130);
                    Cell(file, prefix + "duration_minutes");
                    Cell(file, prefix + "recommended_strength");
                    Cell(file, prefix + "reward_coins");
                    Cell(file, prefix + "fish_find_chance");
                }
            }
        }

        private void DrawFieldGroups(IReadOnlyList<GameTexts.BalanceFieldGroup> groups)
        {
            foreach (var group in groups)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField(group.Title, EditorStyles.boldLabel);
                foreach (var field in group.Fields)
                {
                    Number(field.File, field.Path, field.Label, 0);
                }
            }
        }

        // ------------------------------------------------------------------ field helpers

        private void Number(string file, string path, string label, double minimum)
        {
            if (!(_documents[file].SelectToken(path) is JValue value))
            {
                return;
            }

            EditorGUI.BeginChangeCheck();
            if (value.Type == JTokenType.Integer)
            {
                var result = EditorGUILayout.LongField(label, value.Value<long>());
                if (EditorGUI.EndChangeCheck())
                {
                    value.Value = (long)Math.Max(minimum, result);
                    _dirty.Add(file);
                }
            }
            else if (value.Type == JTokenType.Float)
            {
                var result = EditorGUILayout.DoubleField(label, value.Value<double>());
                if (EditorGUI.EndChangeCheck())
                {
                    value.Value = Math.Max(minimum, result);
                    _dirty.Add(file);
                }
            }
            else
            {
                EditorGUI.EndChangeCheck();
            }
        }

        /// <summary>A compact numeric cell for tables.</summary>
        private void Cell(string file, string path)
        {
            if (!(_documents[file].SelectToken(path) is JValue value))
            {
                GUILayout.Label("—", GUILayout.Width(70));
                return;
            }

            EditorGUI.BeginChangeCheck();
            if (value.Type == JTokenType.Integer)
            {
                var result = EditorGUILayout.LongField(value.Value<long>(), GUILayout.Width(70));
                if (EditorGUI.EndChangeCheck())
                {
                    value.Value = Math.Max(0L, result);
                    _dirty.Add(file);
                }
            }
            else
            {
                var result = EditorGUILayout.DoubleField(value.Value<double>(), GUILayout.Width(70));
                if (EditorGUI.EndChangeCheck())
                {
                    value.Value = Math.Max(0.0, result);
                    _dirty.Add(file);
                }
            }
        }

        private void Text(string file, string path, float width)
        {
            if (!(_documents[file].SelectToken(path) is JValue value))
            {
                return;
            }

            EditorGUI.BeginChangeCheck();
            var result = EditorGUILayout.TextField(value.Value<string>(), GUILayout.Width(width));
            if (EditorGUI.EndChangeCheck())
            {
                value.Value = result;
                _dirty.Add(file);
            }
        }

        private static void Header(IEnumerable<string> columns)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var first = true;
                foreach (var column in columns)
                {
                    GUILayout.Label(column, EditorStyles.miniBoldLabel, GUILayout.Width(first ? 130 : 70));
                    first = false;
                }
            }
        }

        private string SpeciesName(string id)
        {
            var species = _documents[GameConfigLoader.FishCatalogFile]["species"]?.FirstOrDefault(s => s.Value<string>("id") == id);
            return species?.Value<string>("display_name") ?? id;
        }

        private string RarityName(string id)
        {
            var tier = _documents[GameConfigLoader.ProgressionFile].SelectToken("rarity.tiers")?.FirstOrDefault(t => t.Value<string>("id") == id);
            return tier?.Value<string>("display_name") ?? id;
        }
    }
}
