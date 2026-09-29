using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.Texts;
using V = FishingIdle.Texts.GameTexts.Validation;

namespace FishingIdle.GameService.Config
{
    /// <summary>
    /// Checks the balance files for values the rules cannot run on. Every message is PT-BR and names
    /// the file and the entry, because the person reading it is editing that file.
    /// </summary>
    internal static class GameConfigValidator
    {
        public static List<string> Validate(
            FishCatalogConfig fishCatalog,
            MapsConfig maps,
            ProgressionConfig progression,
            RodsConfig rods,
            EconomyConfig economy,
            ArenaConfig arena,
            ExpeditionsConfig expeditions)
        {
            var errors = new List<string>();

            // ---- progression.json first: species and maps refer to its rarities.
            var rarityIds = new HashSet<string>(StringComparer.Ordinal);
            if (progression.Rarity?.Tiers == null || progression.Rarity.Tiers.Count == 0)
            {
                errors.Add(V.Missing(GameConfigLoader.ProgressionFile, "rarity.tiers"));
            }
            else
            {
                foreach (var tier in progression.Rarity.Tiers)
                {
                    if (string.IsNullOrWhiteSpace(tier.Id) || !rarityIds.Add(tier.Id))
                    {
                        errors.Add(V.DuplicateOrEmptyId(GameConfigLoader.ProgressionFile, "rarity.tiers", tier.Id));
                    }

                    if (tier.FisherXpMultiplier < 0 || tier.SaleValueMultiplier < 0)
                    {
                        errors.Add(V.NegativeValue(GameConfigLoader.ProgressionFile, "rarity " + tier.Id));
                    }
                }
            }

            var fishing = progression.Fishing;
            if (fishing == null)
            {
                errors.Add(V.Missing(GameConfigLoader.ProgressionFile, "fishing"));
            }
            else
            {
                if (fishing.OnlineCycleSeconds < 1)
                {
                    errors.Add(V.AtLeast(GameConfigLoader.ProgressionFile, "fishing.online_cycle_seconds", 1));
                }

                if (fishing.OfflineCycleSeconds < 1)
                {
                    errors.Add(V.AtLeast(GameConfigLoader.ProgressionFile, "fishing.offline_cycle_seconds", 1));
                }

                if (fishing.OfflineAccumulationCapHours < 0)
                {
                    errors.Add(V.NegativeValue(GameConfigLoader.ProgressionFile, "fishing.offline_accumulation_cap_hours"));
                }
            }

            var categoryIds = new HashSet<string>(StringComparer.Ordinal);
            if (progression.Size?.Categories == null || progression.Size.Categories.Count == 0)
            {
                errors.Add(V.Missing(GameConfigLoader.ProgressionFile, "size.categories"));
            }
            else
            {
                foreach (var category in progression.Size.Categories)
                {
                    if (string.IsNullOrWhiteSpace(category.Id) || !categoryIds.Add(category.Id))
                    {
                        errors.Add(V.DuplicateOrEmptyId(GameConfigLoader.ProgressionFile, "size.categories", category.Id));
                    }

                    if (category.DrawWeight < 0)
                    {
                        errors.Add(V.NegativeValue(GameConfigLoader.ProgressionFile, "size " + category.Id + " draw_weight"));
                    }

                    if (category.PercentileMin < 0 || category.PercentileMax > 1 || category.PercentileMin >= category.PercentileMax)
                    {
                        errors.Add(V.BadPercentileBand(category.Id));
                    }

                    if (category.FisherXpMultiplier < 0)
                    {
                        errors.Add(V.NegativeValue(GameConfigLoader.ProgressionFile, "size " + category.Id + " fisher_xp_multiplier"));
                    }
                }

                if (progression.Size.Categories.Sum(c => Math.Max(0, c.DrawWeight)) <= 0)
                {
                    errors.Add(V.WeightsSumToZero(GameConfigLoader.ProgressionFile, "size.categories"));
                }
            }

            if (progression.Size?.SaleValueInfluence == null)
            {
                errors.Add(V.Missing(GameConfigLoader.ProgressionFile, "size.sale_value_influence"));
            }

            var fisher = progression.Fisher;
            if (fisher == null || fisher.XpTable == null)
            {
                errors.Add(V.Missing(GameConfigLoader.ProgressionFile, "fisher.xp_table"));
            }
            else
            {
                if (fisher.MaxLevel < 2)
                {
                    errors.Add(V.AtLeast(GameConfigLoader.ProgressionFile, "fisher.max_level", 2));
                }

                var levels = new HashSet<int>(fisher.XpTable.Select(x => x.Level));
                if (levels.Count != fisher.XpTable.Count)
                {
                    errors.Add(V.DuplicateOrEmptyId(GameConfigLoader.ProgressionFile, "fisher.xp_table", "level"));
                }

                for (var level = 1; level < fisher.MaxLevel; level++)
                {
                    if (!levels.Contains(level))
                    {
                        errors.Add(V.XpTableGap(level));
                        break;
                    }
                }

                if (fisher.XpTable.Any(x => x.XpToNextLevel <= 0))
                {
                    errors.Add(V.AtLeast(GameConfigLoader.ProgressionFile, "fisher.xp_table xp_to_next_level", 1));
                }
            }

            var fishLevel = progression.FishLevel;
            if (fishLevel == null || fishLevel.XpTable == null)
            {
                errors.Add(V.Missing(GameConfigLoader.ProgressionFile, "fish_level.xp_table"));
            }
            else
            {
                if (fishLevel.MaxLevel < 1)
                {
                    errors.Add(V.AtLeast(GameConfigLoader.ProgressionFile, "fish_level.max_level", 1));
                }

                var fishLevels = new HashSet<int>(fishLevel.XpTable.Select(x => x.Level));
                if (fishLevels.Count != fishLevel.XpTable.Count)
                {
                    errors.Add(V.DuplicateOrEmptyId(GameConfigLoader.ProgressionFile, "fish_level.xp_table", "level"));
                }

                for (var level = 1; level < fishLevel.MaxLevel; level++)
                {
                    if (!fishLevels.Contains(level))
                    {
                        errors.Add(V.FishXpTableGap(level));
                        break;
                    }
                }

                if (fishLevel.XpTable.Any(x => x.XpToNextLevel <= 0))
                {
                    errors.Add(V.AtLeast(GameConfigLoader.ProgressionFile, "fish_level.xp_table xp_to_next_level", 1));
                }

                if (fishLevel.StatBonusPerLevelPercent < 0)
                {
                    errors.Add(V.NegativeValue(GameConfigLoader.ProgressionFile, "fish_level.stat_bonus_per_level_percent"));
                }
            }

            if (progression.Feeding == null || progression.Feeding.ValuableFeedRules == null)
            {
                errors.Add(V.Missing(GameConfigLoader.ProgressionFile, "feeding"));
            }
            else if (progression.Feeding.InvestedXpRecoveryRatio < 0 || progression.Feeding.InvestedXpRecoveryRatio > 1)
            {
                errors.Add(V.ChanceOutOfRange(GameConfigLoader.ProgressionFile, "feeding.invested_xp_recovery_ratio"));
            }

            if (progression.Size?.StatInfluence == null)
            {
                errors.Add(V.Missing(GameConfigLoader.ProgressionFile, "size.stat_influence"));
            }

            if (progression.Size?.FeedXpInfluence == null)
            {
                errors.Add(V.Missing(GameConfigLoader.ProgressionFile, "size.feed_xp_influence"));
            }

            // ---- fish_catalog.json
            var speciesById = new Dictionary<string, SpeciesConfig>(StringComparer.Ordinal);
            if (fishCatalog.Species == null || fishCatalog.Species.Count == 0)
            {
                errors.Add(V.Missing(GameConfigLoader.FishCatalogFile, "species"));
            }
            else
            {
                foreach (var species in fishCatalog.Species)
                {
                    if (string.IsNullOrWhiteSpace(species.Id) || speciesById.ContainsKey(species.Id))
                    {
                        errors.Add(V.DuplicateOrEmptyId(GameConfigLoader.FishCatalogFile, "species", species.Id));
                        continue;
                    }

                    speciesById[species.Id] = species;

                    if (string.IsNullOrWhiteSpace(species.DisplayName))
                    {
                        errors.Add(V.Missing(GameConfigLoader.FishCatalogFile, species.Id + ".display_name"));
                    }

                    if (!rarityIds.Contains(species.Rarity ?? string.Empty))
                    {
                        errors.Add(V.UnknownRarity(species.Id, species.Rarity));
                    }

                    if (species.BaseStats == null || species.BaseStats.Hp <= 0 || species.BaseStats.Attack < 0 || species.BaseStats.Defense < 0 || species.BaseStats.Speed <= 0)
                    {
                        errors.Add(V.BadBaseStats(species.Id));
                    }

                    if (species.SizeCm == null || species.SizeCm.Min <= 0 || species.SizeCm.Max <= species.SizeCm.Min)
                    {
                        errors.Add(V.BadSizeRange(species.Id));
                    }

                    if (species.BaseSaleValueCoins < 0 || species.BaseFisherXp < 0 || species.BaseFeedXp < 0)
                    {
                        errors.Add(V.NegativeValue(GameConfigLoader.FishCatalogFile, species.Id));
                    }
                }
            }

            // ---- maps.json
            if (maps.Maps == null || maps.Maps.Count == 0)
            {
                errors.Add(V.Missing(GameConfigLoader.MapsFile, "maps"));
            }
            else
            {
                var mapIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var map in maps.Maps)
                {
                    if (string.IsNullOrWhiteSpace(map.Id) || !mapIds.Add(map.Id))
                    {
                        errors.Add(V.DuplicateOrEmptyId(GameConfigLoader.MapsFile, "maps", map.Id));
                        continue;
                    }

                    var available = new HashSet<string>(map.AvailableRarities ?? new List<string>(), StringComparer.Ordinal);
                    if (map.FishPool == null || map.FishPool.Count == 0)
                    {
                        errors.Add(V.Missing(GameConfigLoader.MapsFile, map.Id + ".fish_pool"));
                        continue;
                    }

                    foreach (var entry in map.FishPool)
                    {
                        if (!speciesById.TryGetValue(entry.SpeciesId ?? string.Empty, out var species))
                        {
                            errors.Add(V.PoolUnknownSpecies(map.Id, entry.SpeciesId));
                            continue;
                        }

                        if (entry.CatchWeight < 0)
                        {
                            errors.Add(V.NegativeValue(GameConfigLoader.MapsFile, map.Id + " → " + entry.SpeciesId));
                        }

                        if (!available.Contains(species.Rarity ?? string.Empty))
                        {
                            errors.Add(V.PoolRarityNotAvailable(map.Id, entry.SpeciesId, species.Rarity));
                        }
                    }

                    if (map.FishPool.Sum(e => Math.Max(0, e.CatchWeight)) <= 0)
                    {
                        errors.Add(V.WeightsSumToZero(GameConfigLoader.MapsFile, map.Id + ".fish_pool"));
                    }
                }
            }

            // ---- rods.json
            if (rods.Rods == null || rods.Rods.Count == 0)
            {
                errors.Add(V.Missing(GameConfigLoader.RodsFile, "rods"));
            }
            else
            {
                var rodIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var rod in rods.Rods)
                {
                    if (string.IsNullOrWhiteSpace(rod.Id) || !rodIds.Add(rod.Id))
                    {
                        errors.Add(V.DuplicateOrEmptyId(GameConfigLoader.RodsFile, "rods", rod.Id));
                        continue;
                    }

                    if (rod.CanCatchRarities == null || rod.CanCatchRarities.Count == 0)
                    {
                        errors.Add(V.Missing(GameConfigLoader.RodsFile, rod.Id + ".can_catch_rarities"));
                    }

                    if (rod.Acquisition == null)
                    {
                        errors.Add(V.Missing(GameConfigLoader.RodsFile, rod.Id + ".acquisition"));
                    }
                    else if (rod.Acquisition.PurchaseCostCoins < 0 || rod.Acquisition.UnlockFisherLevel < 1)
                    {
                        errors.Add(V.NegativeValue(GameConfigLoader.RodsFile, rod.Id + ".acquisition"));
                    }

                    if (rod.HasInternalLevels)
                    {
                        var levelCount = rod.BonusesPerLevel?.RarityEfficiency?.Count ?? 0;
                        for (var level = 2; level <= levelCount; level++)
                        {
                            var step = rod.UpgradeCosts?.FirstOrDefault(c => c.ToLevel == level);
                            if (step == null || step.CostCoins <= 0)
                            {
                                errors.Add(V.RodUpgradeCostMissing(rod.Id, level));
                                break;
                            }
                        }
                    }

                    if (rod.HasInternalLevels)
                    {
                        var perLevel = rod.BonusesPerLevel;
                        if (perLevel == null || perLevel.RarityEfficiency == null || perLevel.SizeQuality == null || perLevel.ShellYield == null)
                        {
                            errors.Add(V.Missing(GameConfigLoader.RodsFile, rod.Id + ".bonuses_per_level"));
                        }
                        else if (perLevel.RarityEfficiency.Concat(perLevel.SizeQuality).Concat(perLevel.ShellYield).Any(b => b < 0))
                        {
                            errors.Add(V.NegativeValue(GameConfigLoader.RodsFile, rod.Id + ".bonuses_per_level"));
                        }
                    }
                    else if (rod.Bonuses != null && (rod.Bonuses.RarityEfficiency < 0 || rod.Bonuses.SizeQuality < 0 || rod.Bonuses.ShellYield < 0))
                    {
                        errors.Add(V.NegativeValue(GameConfigLoader.RodsFile, rod.Id + ".bonuses"));
                    }
                }
            }

            // ---- economy.json
            if (economy.NpcFishSale == null)
            {
                errors.Add(V.Missing(GameConfigLoader.EconomyFile, "npc_fish_sale"));
            }
            else if (economy.NpcFishSale.MinimumPriceCoins < 0)
            {
                errors.Add(V.NegativeValue(GameConfigLoader.EconomyFile, "npc_fish_sale.minimum_price_coins"));
            }

            if (economy.Shells == null || economy.Shells.AmountPerDrop == null)
            {
                errors.Add(V.Missing(GameConfigLoader.EconomyFile, "shells"));
            }
            else
            {
                if (economy.Shells.BaseDropChancePerCatch < 0 || economy.Shells.BaseDropChancePerCatch > 1)
                {
                    errors.Add(V.ChanceOutOfRange(GameConfigLoader.EconomyFile, "shells.base_drop_chance_per_catch"));
                }

                if (economy.Shells.AmountPerDrop.Min < 0 || economy.Shells.AmountPerDrop.Max < economy.Shells.AmountPerDrop.Min)
                {
                    errors.Add(V.BadIntRange(GameConfigLoader.EconomyFile, "shells.amount_per_drop"));
                }
            }

            if (economy.Aquarium == null || economy.Aquarium.HardCapacity < 1)
            {
                errors.Add(V.AtLeast(GameConfigLoader.EconomyFile, "aquarium.hard_capacity", 1));
            }

            if (economy.FishingBox?.BulkSaleProtection == null)
            {
                errors.Add(V.Missing(GameConfigLoader.EconomyFile, "fishing_box.bulk_sale_protection"));
            }

            // ---- arena.json (Cardume parts used since Milestone 3)
            var cardume = arena.Cardume;
            if (cardume == null || cardume.CompleteBonus == null)
            {
                errors.Add(V.Missing(GameConfigLoader.ArenaFile, "cardume"));
            }
            else
            {
                if (cardume.MaxFish < 1 || cardume.MinFish < 0 || cardume.MinFish > cardume.MaxFish)
                {
                    errors.Add(V.BadIntRange(GameConfigLoader.ArenaFile, "cardume.min_fish / max_fish"));
                }

                var bonus = cardume.CompleteBonus;
                if (bonus.HpPercent < 0 || bonus.AttackPercent < 0 || bonus.DefensePercent < 0 || bonus.SpeedPercent < 0)
                {
                    errors.Add(V.NegativeValue(GameConfigLoader.ArenaFile, "cardume.complete_bonus"));
                }
            }

            var weights = arena.CardumeStrength?.Weights;
            if (weights == null)
            {
                errors.Add(V.Missing(GameConfigLoader.ArenaFile, "cardume_strength.weights"));
            }
            else if (weights.HpDivisor <= 0 || weights.Attack < 0 || weights.Defense < 0 || weights.Speed < 0 || arena.CardumeStrength.DisplayScale <= 0)
            {
                errors.Add(V.AtLeast(GameConfigLoader.ArenaFile, "cardume_strength (pesos e escala)", 0));
            }

            // ---- expeditions.json (Milestone 6)
            var eff = expeditions.Efficiency;
            if (eff?.BelowRecommended == null || eff.AboveRecommended == null)
            {
                errors.Add(V.Missing(GameConfigLoader.ExpeditionsFile, "efficiency"));
            }
            else if (eff.BelowRecommended.Floor < 0 || eff.BelowRecommended.Exponent < 0 || eff.AboveRecommended.Slope < 0 || eff.AboveRecommended.Cap < eff.AtRecommendedMultiplier)
            {
                errors.Add(V.BadIntRange(GameConfigLoader.ExpeditionsFile, "efficiency"));
            }

            if (expeditions.Expeditions == null || expeditions.Expeditions.Count == 0)
            {
                errors.Add(V.Missing(GameConfigLoader.ExpeditionsFile, "expeditions"));
            }
            else
            {
                var expIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var e in expeditions.Expeditions)
                {
                    if (string.IsNullOrWhiteSpace(e.Id) || !expIds.Add(e.Id))
                    {
                        errors.Add(V.DuplicateOrEmptyId(GameConfigLoader.ExpeditionsFile, "expeditions", e.Id));
                    }

                    if (e.DurationMinutes <= 0 || e.RecommendedStrength <= 0 || e.RewardCoins < 0)
                    {
                        errors.Add(V.AtLeast(GameConfigLoader.ExpeditionsFile, e.Id + " (duração, força recomendada)", 1));
                    }

                    if (e.FishFindChance < 0 || e.FishFindChance > 1)
                    {
                        errors.Add(V.ChanceOutOfRange(GameConfigLoader.ExpeditionsFile, e.Id + ".fish_find_chance"));
                    }
                }
            }

            return errors;
        }
    }
}
