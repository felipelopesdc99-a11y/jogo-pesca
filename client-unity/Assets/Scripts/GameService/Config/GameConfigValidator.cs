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
            EconomyConfig economy)
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

            if (economy.FishingBox?.BulkSaleProtection == null)
            {
                errors.Add(V.Missing(GameConfigLoader.EconomyFile, "fishing_box.bulk_sale_protection"));
            }

            return errors;
        }
    }
}
