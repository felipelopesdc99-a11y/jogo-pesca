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
            ExpeditionsConfig expeditions,
            ArenaBotsConfig bots,
            MarketBotsConfig marketBots,
            EquipmentConfig equipment,
            PassConfig pass)
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

                    if (category.StatMultiplier <= 0)
                    {
                        errors.Add(V.NegativeValue(GameConfigLoader.ProgressionFile, "size " + category.Id + " stat_multiplier"));
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

                // Per-rarity size multipliers must name real size categories and never be negative.
                foreach (var tier in progression.Rarity?.Tiers ?? new List<RarityTierConfig>())
                {
                    if (tier.SizeWeightMultipliers == null)
                    {
                        continue;
                    }

                    foreach (var pair in tier.SizeWeightMultipliers)
                    {
                        if (!categoryIds.Contains(pair.Key))
                        {
                            errors.Add(V.UnknownSizeInRarity(tier.Id, pair.Key));
                        }

                        if (pair.Value < 0)
                        {
                            errors.Add(V.NegativeValue(GameConfigLoader.ProgressionFile, "rarity " + tier.Id + " size_weight_multipliers." + pair.Key));
                        }
                    }
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

                if (fisher.XpCurveVersion < 0)
                {
                    errors.Add(V.AtLeast(GameConfigLoader.ProgressionFile, "fisher.xp_curve_version", 0));
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

                if (fishLevel.MaxLevel > 1000 || fishLevel.StatBonusCurveExponent < 0.1 || fishLevel.StatBonusCurveExponent > 10)
                {
                    errors.Add(V.LevelCurveInvalid(GameConfigLoader.ProgressionFile, "fish_level"));
                }

                if (fishLevel.StatBonusAtMaxLevelPercent < 0)
                {
                    errors.Add(V.NegativeValue(GameConfigLoader.ProgressionFile, "fish_level.stat_bonus_at_max_level_percent"));
                }

                if (!LegacyLevelsValid(fishLevel.LegacyLevelsV1, fishLevel.MaxLevel))
                {
                    errors.Add(V.LegacyLevelsInvalid(GameConfigLoader.ProgressionFile, "fish_level"));
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

            // ---- genes (M24-T52, A-176)
            var genes = progression.Genes;
            var topStars = 0;
            if (genes == null)
            {
                errors.Add(V.Missing(GameConfigLoader.ProgressionFile, "genes"));
            }
            else if (!GenesValid(genes, out topStars))
            {
                errors.Add(V.GenesInvalid);
            }

            if (genes != null && topStars > 0)
            {
                var feedStars = progression.Feeding?.ValuableFeedRules?.GeneStarsAtOrAbove ?? 0;
                var saleStars = economy?.FishingBox?.BulkSaleProtection?.GeneStarsAtOrAbove ?? 0;
                if (feedStars < 0 || feedStars > topStars || saleStars < 0 || saleStars > topStars)
                {
                    errors.Add(V.GeneProtectionInvalid);
                }
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
            var rodRules = rods.UpgradeRules;
            if (rodRules?.InternalLevels == null || rodRules.InternalLevels.Max < 1 || rodRules.InternalLevels.Max > 1000
                || rodRules.BonusCurveExponent < 0.1 || rodRules.BonusCurveExponent > 10)
            {
                errors.Add(V.LevelCurveInvalid(GameConfigLoader.RodsFile, "upgrade_rules"));
            }

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
                    else
                    {
                        foreach (var rarity in rod.CanCatchRarities.Where(r => !rarityIds.Contains(r ?? string.Empty)))
                        {
                            errors.Add(V.UnknownRodRarity(rod.Id, rarity));
                        }
                    }

                    if (rod.Acquisition == null)
                    {
                        errors.Add(V.Missing(GameConfigLoader.RodsFile, rod.Id + ".acquisition"));
                    }
                    else if (!GameConfig.IsFragmentRod(rod) && !GameConfig.IsFreeClaimRod(rod))
                    {
                        errors.Add(V.RodMethodUnknown(rod.Id, rod.Acquisition.Method));
                    }
                    else if (rod.Acquisition.MarketReferenceCoins < 0)
                    {
                        errors.Add(V.NegativeValue(GameConfigLoader.RodsFile, rod.Id + ".acquisition.market_reference_coins"));
                    }

                    if (rod.HasInternalLevels)
                    {
                        // Levels on a curve (M24-T13): Nv.1 and max bonuses, a price per level, the old-level map.
                        var maxLevel = rods.UpgradeRules?.InternalLevels?.Max ?? 0;
                        if (rod.BonusesAtLevel1 == null || rod.BonusesAtMaxLevel == null)
                        {
                            errors.Add(V.Missing(GameConfigLoader.RodsFile, rod.Id + ".bonuses_at_level_1 / bonuses_at_max_level"));
                        }
                        else if (!RodBonusesValid(rod.BonusesAtLevel1, rod.BonusesAtMaxLevel))
                        {
                            errors.Add(V.LevelBonusInvalid(GameConfigLoader.RodsFile, rod.Id));
                        }

                        if (!LevelCostValid(rod.UpgradeCost, maxLevel))
                        {
                            errors.Add(V.LevelCostInvalid(GameConfigLoader.RodsFile, rod.Id));
                        }

                        if (!LegacyLevelsValid(rod.LegacyLevelsV1, maxLevel))
                        {
                            errors.Add(V.LegacyLevelsInvalid(GameConfigLoader.RodsFile, rod.Id));
                        }
                    }
                    else if (rod.Bonuses != null && (rod.Bonuses.RarityEfficiency < 0 || rod.Bonuses.SizeQuality < 0 || rod.Bonuses.ShellYield < 0))
                    {
                        errors.Add(V.NegativeValue(GameConfigLoader.RodsFile, rod.Id + ".bonuses"));
                    }
                }
            }

            ValidateRodFragments(rods, maps, errors);

            // ---- maps × rods: since M24-T58 (A-180) every rod can fish on every map (a map opens only by the Fisher level),
            // so every rod must have something to catch on every map, or fishing would have no species to draw from.
            if (maps.Maps != null && rods.Rods != null)
            {
                foreach (var map in maps.Maps.Where(m => m.FishPool != null))
                {
                    foreach (var rod in rods.Rods.Where(r => r != null && r.CanCatchRarities != null))
                    {
                        var catchable = map.FishPool.Any(e => e.CatchWeight > 0
                            && speciesById.TryGetValue(e.SpeciesId ?? string.Empty, out var s)
                            && map.AvailableRarities != null && map.AvailableRarities.Contains(s.Rarity)
                            && rod.CanCatchRarities.Contains(s.Rarity));
                        if (!catchable)
                        {
                            errors.Add(V.MapRodCatchesNothing(map.Id, rod.Id));
                        }
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
            else if (economy.NpcFishSale.PriceMultiplier <= 0)
            {
                errors.Add(V.NegativeValue(GameConfigLoader.EconomyFile, "npc_fish_sale.price_multiplier"));
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

            var formation = arena.Formation;
            if (formation?.TargetPriority == null || cardume == null)
            {
                errors.Add(V.Missing(GameConfigLoader.ArenaFile, "formation.target_priority"));
            }
            else if (formation.TargetPriority.Count != cardume.MaxFish
                     || formation.TargetPriority.Distinct().Count() != cardume.MaxFish
                     || formation.TargetPriority.Any(p => p < 1 || p > cardume.MaxFish))
            {
                // Every Cardume position exactly once, or a battle could target an empty slot.
                errors.Add(V.TargetPriority(cardume.MaxFish));
            }

            if (cardume != null && cardume.MaxFish > 6)
            {
                errors.Add(V.BadIntRange(GameConfigLoader.ArenaFile, "cardume.max_fish (no máximo 6)"));
            }

            // ---- arena.json (Arena rules, Milestone 7)
            if (arena.Energy == null || arena.Energy.Max < 1 || arena.Energy.RegenerationSecondsPerPoint <= 0 || arena.Energy.CostPerInitiatedAttack < 0)
            {
                errors.Add(V.AtLeast(GameConfigLoader.ArenaFile, "energy", 1));
            }

            if (arena.OpponentSelection == null || arena.OpponentSelection.OpponentsPerSet < 1 || arena.OpponentSelection.RankWindowPercentAbove <= 0 || arena.OpponentSelection.RerollsPerSet < 0)
            {
                errors.Add(V.AtLeast(GameConfigLoader.ArenaFile, "opponent_selection", 1));
            }

            if (arena.Honor == null || arena.Honor.AttackerVictoryGain < 0 || arena.Honor.SuccessfulDefenseGain < 0 || arena.Honor.DefeatLoss < 0)
            {
                errors.Add(V.NegativeValue(GameConfigLoader.ArenaFile, "honor"));
            }

            var combat = arena.Combat;
            if (combat?.DamageRoll == null || combat.DefenseMitigation == null || combat.Speed == null)
            {
                errors.Add(V.Missing(GameConfigLoader.ArenaFile, "combat"));
            }
            else if (combat.DamageRoll.Min <= 0 || combat.DamageRoll.Max < combat.DamageRoll.Min || combat.DefenseMitigation.Constant <= 0
                     || combat.DefenseMitigation.MinimumDamageRatioOfAttack <= 0 || combat.Speed.BaseIntervalSeconds <= 0 || combat.Speed.ReferenceSpeed <= 0)
            {
                errors.Add(V.BadIntRange(GameConfigLoader.ArenaFile, "combat"));
            }

            // ---- arena_bots.json (local Arena opponents)
            if (bots.BotCount < 3 || bots.Names == null || bots.Names.Count == 0 || bots.StrengthByRank == null || bots.IncomingAttacks == null)
            {
                errors.Add(V.AtLeast(GameConfigLoader.ArenaBotsFile, "bot_count / names / strength_by_rank / incoming_attacks", 3));
            }
            else
            {
                var st = bots.StrengthByRank;
                if (st.TopFishLevel < 1 || st.BottomFishLevel < 1 || st.TopCardumeSize < 1 || st.BottomCardumeSize < 1
                    || st.TopCardumeSize > 6 || st.BottomCardumeSize > 6 || st.TopSizePercentile < 0 || st.TopSizePercentile > 1
                    || st.BottomSizePercentile < 0 || st.BottomSizePercentile > 1)
                {
                    errors.Add(V.BadIntRange(GameConfigLoader.ArenaBotsFile, "strength_by_rank"));
                }

                if (bots.IncomingAttacks.ChancePerCheck < 0 || bots.IncomingAttacks.ChancePerCheck > 1)
                {
                    errors.Add(V.ChanceOutOfRange(GameConfigLoader.ArenaBotsFile, "incoming_attacks.chance_per_check"));
                }

                if (bots.IncomingAttacks.CheckIntervalMinutes <= 0)
                {
                    errors.Add(V.AtLeast(GameConfigLoader.ArenaBotsFile, "incoming_attacks.check_interval_minutes", 1));
                }
            }

            // ---- economy.json → currency_trade (A-101)
            var trade = economy.CurrencyTrade;
            if (trade == null)
            {
                errors.Add(V.Missing(GameConfigLoader.EconomyFile, "currency_trade"));
            }
            else if (trade.MaxActiveListingsPerPlayer < 1 || trade.MinimumPriceCoins < 1)
            {
                errors.Add(V.AtLeast(GameConfigLoader.EconomyFile, "currency_trade (anúncios, preço mínimo)", 1));
            }
            else if (trade.CompletedSaleFeeRatio < 0 || trade.CompletedSaleFeeRatio >= 1)
            {
                errors.Add(V.NegativeValue(GameConfigLoader.EconomyFile, "currency_trade.completed_sale_fee_ratio"));
            }

            // ---- economy.json → vip (A-110)
            var vip = economy.Vip;
            if (vip == null)
            {
                errors.Add(V.Missing(GameConfigLoader.EconomyFile, "vip"));
            }
            else if (vip.PriceDollars < 1 || vip.DurationDays <= 0)
            {
                errors.Add(V.AtLeast(GameConfigLoader.EconomyFile, "vip (preço em Dólares, dias)", 1));
            }
            else if (vip.OfflineFisherXpBonus < 0 || vip.OfflineFisherXpBonus > 10)
            {
                errors.Add(V.NegativeValue(GameConfigLoader.EconomyFile, "vip.offline_fisher_xp_bonus"));
            }
            else if (vip.OfflineCycleSeconds != 0 && (vip.OfflineCycleSeconds < 1 || double.IsNaN(vip.OfflineCycleSeconds)))
            {
                errors.Add(V.AtLeast(GameConfigLoader.EconomyFile, "vip.offline_cycle_seconds", 1));
            }

            // ---- economy.json → market_fixed_price (Milestone 8)
            var market = economy.MarketFixedPrice;
            if (market == null)
            {
                errors.Add(V.Missing(GameConfigLoader.EconomyFile, "market_fixed_price"));
            }
            else
            {
                if (market.MaxActiveListingsPerPlayer < 1 || market.MaxListingDurationDays <= 0 || market.MinimumListingPriceCoins < 1)
                {
                    errors.Add(V.AtLeast(GameConfigLoader.EconomyFile, "market_fixed_price (anúncios, dias, preço mínimo)", 1));
                }

                if (market.ListingFeeCoins < 0)
                {
                    errors.Add(V.NegativeValue(GameConfigLoader.EconomyFile, "market_fixed_price.listing_fee_coins"));
                }

                if (market.CompletedSaleFeeRatio < 0 || market.CompletedSaleFeeRatio >= 1)
                {
                    errors.Add(V.ChanceOutOfRange(GameConfigLoader.EconomyFile, "market_fixed_price.completed_sale_fee_ratio"));
                }
            }

            // ---- economy.json → auction (Milestone 9)
            var auction = economy.Auction;
            if (auction == null)
            {
                errors.Add(V.Missing(GameConfigLoader.EconomyFile, "auction"));
            }
            else
            {
                if (auction.MaxActiveAuctionsPerSeller < 1 || auction.DurationHours <= 0 || auction.AntiSnipeWindowSeconds < 0 || auction.AntiSnipeResetToSeconds < 0)
                {
                    errors.Add(V.AtLeast(GameConfigLoader.EconomyFile, "auction (leilões por vendedor, duração)", 1));
                }

                if (auction.MinBidIncrementRatio <= 0 || auction.MinBidIncrementRatio >= 1 || auction.BidFeeRatio < 0 || auction.BidFeeRatio >= 1
                    || auction.SellerEarlyCloseFeeRatio < 0 || auction.SellerEarlyCloseFeeRatio >= 1 || auction.TimedCompletionFeeRatio < 0 || auction.TimedCompletionFeeRatio >= 1)
                {
                    errors.Add(V.ChanceOutOfRange(GameConfigLoader.EconomyFile, "auction (taxas e aumento mínimo)"));
                }
            }

            // ---- market_bots.json (local simulated Market)
            if (marketBots.Valuation == null || marketBots.Supply?.PriceRatio == null || marketBots.Demand == null)
            {
                errors.Add(V.Missing(GameConfigLoader.MarketBotsFile, "valuation / supply / demand"));
            }
            else
            {
                var val = marketBots.Valuation;
                if (val.FishReferenceRatio <= 0 || val.RodReferenceRatio <= 0 || val.FishLevelPremiumPerLevel < 0)
                {
                    errors.Add(V.NegativeValue(GameConfigLoader.MarketBotsFile, "valuation"));
                }

                var sup = marketBots.Supply;
                if (sup.TargetListingCount < 0 || sup.NewListingsPerRefresh < 0 || sup.RefreshIntervalMinutes <= 0 || sup.ListingDurationHours <= 0
                    || sup.MaxFishLevel < 1 || sup.MaxRodLevel < 1 || sup.PriceRatio.Min <= 0 || sup.PriceRatio.Max < sup.PriceRatio.Min)
                {
                    errors.Add(V.BadIntRange(GameConfigLoader.MarketBotsFile, "supply"));
                }

                if (sup.RodListingChance < 0 || sup.RodListingChance > 1)
                {
                    errors.Add(V.ChanceOutOfRange(GameConfigLoader.MarketBotsFile, "supply.rod_listing_chance"));
                }

                var dem = marketBots.Demand;
                if (dem.CheckIntervalMinutes <= 0 || dem.MaxPriceRatio <= 1 || dem.MaxChecksPerCatchUp < 1)
                {
                    errors.Add(V.BadIntRange(GameConfigLoader.MarketBotsFile, "demand"));
                }

                if (dem.ChanceAtReferencePrice < 0 || dem.ChanceAtReferencePrice > 1)
                {
                    errors.Add(V.ChanceOutOfRange(GameConfigLoader.MarketBotsFile, "demand.chance_at_reference_price"));
                }
            }

            var au = marketBots.Auctions;
            if (au?.StartingBidRatio == null || au.ExtraRaiseRatio == null)
            {
                errors.Add(V.Missing(GameConfigLoader.MarketBotsFile, "auctions"));
            }
            else
            {
                if (au.TargetAuctionCount < 0 || au.NewAuctionsPerRefresh < 0 || au.RefreshIntervalMinutes <= 0 || au.BidCheckIntervalMinutes <= 0
                    || au.MaxChecksPerCatchUp < 1 || au.StartingBidRatio.Min <= 0 || au.StartingBidRatio.Max < au.StartingBidRatio.Min
                    || au.ExtraRaiseRatio.Min < 0 || au.ExtraRaiseRatio.Max < au.ExtraRaiseRatio.Min || au.MaxBidRatio <= 0)
                {
                    errors.Add(V.BadIntRange(GameConfigLoader.MarketBotsFile, "auctions"));
                }

                if (au.RodAuctionChance < 0 || au.RodAuctionChance > 1 || au.BidChancePerCheck < 0 || au.BidChancePerCheck > 1)
                {
                    errors.Add(V.ChanceOutOfRange(GameConfigLoader.MarketBotsFile, "auctions (chances)"));
                }
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

            // ---- Catch Success (docs/SISTEMA_SUCESSO_PESCA.md)
            foreach (var tier in progression.Rarity?.Tiers ?? new List<RarityTierConfig>())
            {
                if (tier.CatchSuccessBase < 0 || tier.CatchSuccessBase > 1)
                {
                    errors.Add(V.ChanceOutOfRange(GameConfigLoader.ProgressionFile, "rarity " + tier.Id + " catch_success_base"));
                }

                // M24-T22 (A-159): the share of the plain chance a rarity keeps while fishing.
                if (tier.FishingBiteMultiplier < 0 || double.IsNaN(tier.FishingBiteMultiplier))
                {
                    errors.Add(V.NegativeValue(GameConfigLoader.ProgressionFile, "rarity " + tier.Id + " fishing_bite_multiplier"));
                }
            }

            if (progression.Fishing != null && (progression.Fishing.CatchSuccessMin < 0 || progression.Fishing.CatchSuccessMax > 1
                || progression.Fishing.CatchSuccessMin > progression.Fishing.CatchSuccessMax))
            {
                errors.Add(V.ChanceOutOfRange(GameConfigLoader.ProgressionFile, "fishing.catch_success_min / catch_success_max"));
            }

            // ---- equipment.json
            if (equipment?.Boats == null || equipment.Boats.Count == 0)
            {
                errors.Add(V.Missing(GameConfigLoader.EquipmentFile, "boats"));
            }
            else
            {
                var boatIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var boat in equipment.Boats)
                {
                    if (string.IsNullOrWhiteSpace(boat.Id) || !boatIds.Add(boat.Id))
                    {
                        errors.Add(V.DuplicateOrEmptyId(GameConfigLoader.EquipmentFile, "boats", boat.Id));
                    }

                    if (boat.CatchSuccessBonus < 0 || boat.CatchSuccessBonus > 1)
                    {
                        errors.Add(V.ChanceOutOfRange(GameConfigLoader.EquipmentFile, boat.Id + ".catch_success_bonus"));
                    }

                    if (boat.HasLevels)
                    {
                        // Boat levels (M24-T13): the bonus at the max level stays a chance and never below Nv.1.
                        if (boat.CatchSuccessBonusAtMaxLevel < boat.CatchSuccessBonus || boat.CatchSuccessBonusAtMaxLevel > 1)
                        {
                            errors.Add(V.LevelBonusInvalid(GameConfigLoader.EquipmentFile, boat.Id));
                        }

                        if (!LevelCostValid(boat.UpgradeCost, equipment.BoatLevels?.MaxLevel ?? 0))
                        {
                            errors.Add(V.LevelCostInvalid(GameConfigLoader.EquipmentFile, boat.Id));
                        }
                    }
                }

                var boatLevels = equipment.BoatLevels;
                if (equipment.Boats.Any(b => b.HasLevels)
                    && (boatLevels == null || boatLevels.MaxLevel < 1 || boatLevels.MaxLevel > 1000 || boatLevels.BonusCurveExponent < 0.1 || boatLevels.BonusCurveExponent > 10))
                {
                    errors.Add(V.LevelCurveInvalid(GameConfigLoader.EquipmentFile, "boat_levels"));
                }

                ValidateBoatFragments(equipment, maps, errors);
            }

            if (equipment?.Baits == null)
            {
                errors.Add(V.Missing(GameConfigLoader.EquipmentFile, "baits"));
            }
            else
            {
                var baitIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var bait in equipment.Baits)
                {
                    if (string.IsNullOrWhiteSpace(bait.Id) || !baitIds.Add(bait.Id))
                    {
                        errors.Add(V.DuplicateOrEmptyId(GameConfigLoader.EquipmentFile, "baits", bait.Id));
                    }

                    if (bait.CatchSuccessBonus < 0 || bait.CatchSuccessBonus > 1)
                    {
                        errors.Add(V.ChanceOutOfRange(GameConfigLoader.EquipmentFile, bait.Id + ".catch_success_bonus"));
                    }

                    if (bait.Charges < 1)
                    {
                        errors.Add(V.AtLeast(GameConfigLoader.EquipmentFile, bait.Id + ".charges", 1));
                    }

                    if (bait.CostCoins < 0 || bait.CostShells < 0)
                    {
                        errors.Add(V.NegativeValue(GameConfigLoader.EquipmentFile, bait.Id + " (custo)"));
                    }
                }

                ValidateBaitMigration(equipment.BaitMigration, errors);
                ValidateBoatMigration(equipment, errors);
            }

            ValidateCrewRemovalRefund(economy?.CrewRemovalRefund, errors);
            ValidateClothing(equipment?.Clothing, errors);
            ValidatePass(pass, progression, equipment, rods, errors);
            return errors;
        }

        /// <summary>
        /// Rods by fragments (M24-T32, A-163): every rod but the free one has at least 1 fragment to assemble it and a chance
        /// from 0 to 1. Since M24-T47 (A-173) every rod drops on every map; since M24-T58 (A-180) maps open only by the
        /// Fisher level, so no rod is needed to reach a map.
        /// </summary>
        private static void ValidateRodFragments(RodsConfig rods, MapsConfig maps, List<string> errors)
        {
            const string file = GameConfigLoader.RodsFile;
            if (rods?.Rods == null || maps?.Maps == null)
            {
                return;
            }

            var fragmentRods = rods.Rods.Where(r => r != null && GameConfig.IsFragmentRod(r)).ToList();
            foreach (var rod in fragmentRods)
            {
                if (rod.FragmentsRequired < 1 || rod.FragmentsRequired > 1000000)
                {
                    errors.Add(V.AtLeast(file, rod.Id + ".fragments_required", 1));
                }

                if (!(rod.FragmentDropChance >= 0) || rod.FragmentDropChance > 1)
                {
                    errors.Add(V.ChanceOutOfRange(file, rod.Id + ".fragment_drop_chance"));
                }
            }
        }

        /// <summary>
        /// Clothing (M24-T33, A-164): a drop chance, the fragments of each piece, the level curve and the use XP, and pieces
        /// with distinct ids, a known axis and bonuses that never go down.
        /// </summary>
        private static void ValidateClothing(ClothingConfig clothing, List<string> errors)
        {
            if (clothing == null)
            {
                return;
            }

            if (!(clothing.FragmentDropChance >= 0) || clothing.FragmentDropChance > 1)
            {
                errors.Add(V.ChanceOutOfRange(GameConfigLoader.EquipmentFile, "clothing.fragment_drop_chance"));
            }

            if (clothing.FragmentsToAssemble == null || clothing.FragmentsToAssemble.Count == 0 || clothing.FragmentsToAssemble.Any(n => n < 1 || n > 1000000))
            {
                errors.Add(V.ClothingInvalid("fragments_to_assemble"));
            }

            if (clothing.MaxLevel < 1 || clothing.MaxLevel > 1000 || clothing.BonusCurveExponent < 0.1 || clothing.BonusCurveExponent > 10)
            {
                errors.Add(V.LevelCurveInvalid(GameConfigLoader.EquipmentFile, "clothing (max_level / bonus_curve_exponent)"));
            }

            if (clothing.UseXpPerFish < 1)
            {
                errors.Add(V.AtLeast(GameConfigLoader.EquipmentFile, "clothing.use_xp_per_fish", 1));
            }

            var xp = clothing.LevelXp;
            if (xp == null || xp.First < 1 || xp.Growth < 1 || xp.Growth > 10
                || LevelCurve.Cost(xp.First, xp.Growth, Math.Max(1, clothing.MaxLevel - 1)) >= 1_000_000_000_000_000_000L)
            {
                errors.Add(V.ClothingInvalid("level_xp"));
            }

            if (clothing.Pieces == null || clothing.Pieces.Count == 0)
            {
                errors.Add(V.Missing(GameConfigLoader.EquipmentFile, "clothing.pieces"));
                return;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var piece in clothing.Pieces)
            {
                if (piece == null || string.IsNullOrWhiteSpace(piece.Id) || !ids.Add(piece.Id))
                {
                    errors.Add(V.DuplicateOrEmptyId(GameConfigLoader.EquipmentFile, "clothing.pieces", piece?.Id));
                    continue;
                }

                if (!ClothingAxes.All.Contains(piece.BonusAxis))
                {
                    errors.Add(V.ClothingAxisUnknown(piece.Id, piece.BonusAxis));
                }

                if (piece.BonusAtLevel1 < 0 || piece.BonusAtMaxLevel < piece.BonusAtLevel1
                    || (piece.BonusAxis == ClothingAxes.CatchSuccess && piece.BonusAtMaxLevel > 1))
                {
                    errors.Add(V.LevelBonusInvalid(GameConfigLoader.EquipmentFile, "clothing." + piece.Id));
                }
            }
        }

        /// <summary>
        /// Boats by fragments (M24-T16, A-157): the starter boat belongs to no map; every other boat to one existing map
        /// (map_id) and maybe more (extra_fragment_maps, M24-T42), one boat per map (the Boss Mundial's boat, M24-T47), with
        /// at least 1 fragment to assemble it and a drop chance between 0 and 1 (on every map since M24-T47, A-173).
        /// </summary>
        private static void ValidateBoatFragments(EquipmentConfig equipment, MapsConfig maps, List<string> errors)
        {
            const string file = GameConfigLoader.EquipmentFile;
            var mapIds = new HashSet<string>((maps?.Maps ?? new List<MapConfig>()).Where(m => m != null && m.Id != null).Select(m => m.Id), StringComparer.Ordinal);
            var starter = equipment.Boats.OrderBy(b => b.Tier).First();
            var used = new HashSet<string>(StringComparer.Ordinal);
            foreach (var boat in equipment.Boats)
            {
                if (boat == starter)
                {
                    if (!string.IsNullOrEmpty(boat.MapId))
                    {
                        errors.Add(V.StarterBoatHasMap(boat.Id));
                    }

                    continue;
                }

                if (string.IsNullOrWhiteSpace(boat.MapId) || !mapIds.Contains(boat.MapId))
                {
                    errors.Add(V.BoatMapUnknown(boat.Id, boat.MapId));
                }
                else if (!used.Add(boat.MapId))
                {
                    errors.Add(V.BoatMapRepeated(boat.MapId));
                }

                // More maps of this boat (M24-T42, A-168): each one existing and not taken by another boat.
                foreach (var extra in boat.ExtraFragmentMaps ?? new List<BoatFragmentMapConfig>())
                {
                    if (extra == null || string.IsNullOrWhiteSpace(extra.MapId) || !mapIds.Contains(extra.MapId))
                    {
                        errors.Add(V.BoatMapUnknown(boat.Id, extra?.MapId));
                        continue;
                    }

                    if (!used.Add(extra.MapId))
                    {
                        errors.Add(V.BoatMapRepeated(extra.MapId));
                    }
                }

                if (boat.FragmentsRequired < 1 || boat.FragmentsRequired > 1000000)
                {
                    errors.Add(V.AtLeast(file, boat.Id + ".fragments_required", 1));
                }

                if (!(boat.FragmentDropChance >= 0) || boat.FragmentDropChance > 1)
                {
                    errors.Add(V.ChanceOutOfRange(file, boat.Id + ".fragment_drop_chance"));
                }
            }
        }

        /// <summary>
        /// The Passe do Pescador (M24-T17, A-158): a price in Dólares, milestones at distinct levels from 1 to the Fisher's
        /// max level in ascending order, and rewards with nothing negative and fragments only of boats that exist.
        /// </summary>
        private static void ValidatePass(PassConfig pass, ProgressionConfig progression, EquipmentConfig equipment, RodsConfig rods, List<string> errors)
        {
            const string file = GameConfigLoader.PassFile;
            if (pass == null || pass.Milestones == null || pass.Milestones.Count == 0)
            {
                errors.Add(V.Missing(file, "milestones"));
                return;
            }

            if (string.IsNullOrWhiteSpace(pass.PassId))
            {
                errors.Add(V.Missing(file, "pass_id"));
            }

            if (pass.PremiumPriceDollars < 1)
            {
                errors.Add(V.AtLeast(file, "premium_price_dollars", 1));
            }

            var maxLevel = progression?.Fisher?.MaxLevel ?? 0;
            var starter = equipment?.Boats != null && equipment.Boats.Count > 0 ? equipment.Boats.OrderBy(b => b.Tier).First().Id : null;
            var boatIds = new HashSet<string>((equipment?.Boats ?? new List<BoatConfig>()).Where(b => b.Id != null && b.Id != starter).Select(b => b.Id), StringComparer.Ordinal);
            var rodIds = new HashSet<string>((rods?.Rods ?? new List<RodConfig>()).Where(r => r?.Id != null && GameConfig.IsFragmentRod(r)).Select(r => r.Id), StringComparer.Ordinal);
            var previous = 0;
            foreach (var milestone in pass.Milestones)
            {
                if (milestone == null || milestone.Level <= previous || milestone.Level > maxLevel)
                {
                    errors.Add(V.PassLevels(maxLevel));
                    return;
                }

                previous = milestone.Level;
                CheckReward(milestone.Free, milestone.Level);
                CheckReward(milestone.Premium, milestone.Level);
            }

            void CheckReward(PassRewardConfig reward, int level)
            {
                if (reward == null)
                {
                    errors.Add(V.Missing(file, "milestones (Nv." + level + ") free / premium"));
                    return;
                }

                if (reward.Coins < 0 || reward.Shells < 0 || reward.Dollars < 0 || reward.Fragments < 0 || reward.RodFragments < 0 || reward.ClothingFragments < 0)
                {
                    errors.Add(V.NegativeValue(file, "milestones (Nv." + level + ")"));
                }

                if (reward.RodFragments > 0 && (reward.RodId == null || !rodIds.Contains(reward.RodId)))
                {
                    errors.Add(V.PassRodUnknown(level, reward.RodId));
                }

                if (reward.Fragments > 0 && (reward.BoatId == null || !boatIds.Contains(reward.BoatId)))
                {
                    errors.Add(V.PassBoatUnknown(level, reward.BoatId));
                }
            }
        }

        /// <summary>The baits before M24-T30 (A-161), kept to convert old saves: a version, and each old bait a usable price.</summary>
        private static void ValidateBaitMigration(BaitMigrationConfig migration, List<string> errors)
        {
            if (migration == null)
            {
                return;
            }

            if (migration.BaitModelVersion < 1)
            {
                errors.Add(V.BaitMigrationInvalid("bait_model_version"));
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var bait in migration.LegacyBaits ?? new List<BaitConfig>())
            {
                if (bait == null || string.IsNullOrWhiteSpace(bait.Id) || !ids.Add(bait.Id) || bait.CatchSuccessBonus < 0 || bait.CatchSuccessBonus > 1
                    || bait.Charges < 1 || bait.CostCoins < 0 || bait.CostShells < 0)
                {
                    errors.Add(V.BaitMigrationInvalid("legacy_baits (" + (bait?.Id ?? "") + ")"));
                }
            }
        }

        /// <summary>
        /// The boats removed in M24-T42 (A-168), kept to convert old saves: a version, and each removed boat with an id no boat
        /// of today uses, fragments going to a boat of today and a valid level price.
        /// </summary>
        private static void ValidateBoatMigration(EquipmentConfig equipment, List<string> errors)
        {
            var migration = equipment.BoatMigration;
            if (migration == null)
            {
                return;
            }

            if (migration.BoatModelVersion < 1)
            {
                errors.Add(V.BoatMigrationInvalid("boat_model_version"));
            }

            var today = new HashSet<string>(equipment.Boats.Where(b => b != null && b.Id != null).Select(b => b.Id), StringComparer.Ordinal);
            var starter = equipment.Boats.OrderBy(b => b.Tier).First();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var boat in migration.RemovedBoats ?? new List<RemovedBoatConfig>())
            {
                if (boat == null || string.IsNullOrWhiteSpace(boat.Id) || !ids.Add(boat.Id) || today.Contains(boat.Id)
                    || boat.FragmentsGoTo == null || !today.Contains(boat.FragmentsGoTo) || boat.FragmentsGoTo == starter.Id
                    || boat.FragmentsRequired < 0 || boat.UpgradeCost == null || boat.UpgradeCost.CoinsFirst < 0 || boat.UpgradeCost.ShellsFirst < 0
                    || boat.UpgradeCost.CoinsGrowth < 1 || boat.UpgradeCost.CoinsGrowth > 10 || boat.UpgradeCost.ShellsGrowth < 1 || boat.UpgradeCost.ShellsGrowth > 10)
                {
                    errors.Add(V.BoatMigrationInvalid("removed_boats (" + (boat?.Id ?? "") + ")"));
                }
            }
        }

        /// <summary>
        /// A level price (M24-T13): Coins and at least 1 Shell for every level (the owner's rule, M24-T30: every level costs Moedas and
        /// Conchas), growth from 1 to 10, and the last level fits a long with room to spare.
        /// </summary>
        private static bool LevelCostValid(LevelCostConfig cost, int maxLevel)
        {
            if (cost == null || cost.CoinsFirst <= 0 || cost.ShellsFirst < 1
                || cost.CoinsGrowth < 1 || cost.CoinsGrowth > 10 || cost.ShellsGrowth < 1 || cost.ShellsGrowth > 10)
            {
                return false;
            }

            var last = Math.Max(1, maxLevel - 1);
            return LevelCurve.Cost(cost.CoinsFirst, cost.CoinsGrowth, last) < 1_000_000_000_000_000_000L
                && LevelCurve.Cost(cost.ShellsFirst, cost.ShellsGrowth, last) < 1_000_000_000_000_000_000L;
        }

        /// <summary>A rod's Nv.1 and max-level bonuses: none negative, none smaller at the max, the Catch Success a chance.</summary>
        private static bool RodBonusesValid(RodBonusesConfig at1, RodBonusesConfig atMax)
        {
            var low = new[] { at1.RarityEfficiency, at1.SizeQuality, at1.ShellYield, at1.CatchSuccess };
            var high = new[] { atMax.RarityEfficiency, atMax.SizeQuality, atMax.ShellYield, atMax.CatchSuccess };
            return low.All(v => v >= 0) && low.Zip(high, (a, b) => b >= a).All(ok => ok) && atMax.CatchSuccess <= 1;
        }

        /// <summary>The map of the old levels 1–10 (curve 1) to the current curve: 10 levels, never going down, inside 1..max.</summary>
        /// <summary>The genes section (M24-T52): ranges, and star bands 1, 2, 3… with min_percent rising from 0 to at most 100.</summary>
        private static bool GenesValid(GenesConfig genes, out int topStars)
        {
            topStars = 0;
            if (genes.MaxValue < 1 || genes.MaxValue > 255 || genes.StatMultiplierMin <= 0 || genes.StatMultiplierMin > 1 || genes.StatMultiplierMax < 1
                || genes.StatMultiplierNeutralGene < 1 || genes.StatMultiplierNeutralGene >= genes.MaxValue
                || genes.StatMultiplierMax > 10 || genes.MigrationNeutralValue < 0 || genes.MigrationNeutralValue > genes.MaxValue)
            {
                return false;
            }

            var bands = genes.QualityStars;
            if (bands == null || bands.Count == 0 || bands.Count > 10 || bands[0] == null || bands[0].MinPercent != 0)
            {
                return false;
            }

            for (var i = 0; i < bands.Count; i++)
            {
                if (bands[i] == null || bands[i].Stars != i + 1 || bands[i].MinPercent > 100 || (i > 0 && bands[i].MinPercent <= bands[i - 1].MinPercent))
                {
                    return false;
                }
            }

            topStars = bands.Count;
            return genes.CelebrateMinStars >= 0 && genes.CelebrateMinStars <= topStars;
        }

        private static bool LegacyLevelsValid(List<int> legacy, int maxLevel)
        {
            if (legacy == null || legacy.Count != 10 || legacy[0] < 1 || legacy.Any(l => l > maxLevel))
            {
                return false;
            }

            for (var i = 1; i < legacy.Count; i++)
            {
                if (legacy[i] < legacy[i - 1])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// economy.json → crew_removal_refund (M24-T46, A-172): the prices of the removed Crew and Upgrades, used only to give
        /// old saves back what they spent. Optional: without it nothing is refunded and the game still opens.
        /// </summary>
        private static void ValidateCrewRemovalRefund(CrewRemovalRefundConfig refund, List<string> errors)
        {
            if (refund == null)
            {
                return;
            }

            var members = new HashSet<string>(StringComparer.Ordinal);
            foreach (var member in refund.CrewMembers ?? new List<RetiredCrewMemberConfig>())
            {
                if (member == null || string.IsNullOrWhiteSpace(member.Id) || !members.Add(member.Id)
                    || !(member.BaseCost >= 0) || !(member.CostGrowth > 1) || member.CostGrowth > 10)
                {
                    errors.Add(V.CrewRefundInvalid("crew_members (" + (member?.Id ?? "") + ")"));
                }
            }

            foreach (var tier in refund.CrewUpgradeTiers ?? new List<RetiredCrewUpgradeTierConfig>())
            {
                if (tier == null || tier.UnlockCount < 0 || !(tier.CostFactor >= 0))
                {
                    errors.Add(V.CrewRefundInvalid("crew_upgrade_tiers"));
                }
            }

            var general = new HashSet<string>(StringComparer.Ordinal);
            foreach (var upgrade in refund.GeneralUpgrades ?? new List<RetiredGeneralUpgradeConfig>())
            {
                if (upgrade == null || string.IsNullOrWhiteSpace(upgrade.Id) || !general.Add(upgrade.Id)
                    || !(upgrade.BaseCost >= 0) || !(upgrade.CostGrowth >= 1) || upgrade.CostGrowth > 1000 || upgrade.MaxLevel < 0 || upgrade.MaxLevel > 1000)
                {
                    errors.Add(V.CrewRefundInvalid("general_upgrades (" + (upgrade?.Id ?? "") + ")"));
                }
            }
        }

        /// <summary>world_boss.json (M24-T29, A-165): a window inside one day, a positive attack interval, ascending bands.</summary>
        public static List<string> ValidateWorldBoss(WorldBossConfig boss)
        {
            var errors = new List<string>();
            if (boss == null)
            {
                errors.Add(V.ConfigFileMissing(GameConfigLoader.WorldBossFile));
                return errors;
            }

            if (string.IsNullOrWhiteSpace(boss.DisplayName))
            {
                errors.Add(V.Missing(GameConfigLoader.WorldBossFile, "display_name"));
            }

            var schedule = boss.Schedule;
            if (schedule == null || schedule.UtcOffsetMinutes < -720 || schedule.UtcOffsetMinutes > 840
                || schedule.StartMinuteOfDay < 0 || schedule.StartMinuteOfDay > 1439
                || schedule.DurationMinutes < 1 || schedule.DurationMinutes > 240
                || schedule.StartMinuteOfDay + schedule.DurationMinutes > 1440)
            {
                errors.Add(V.WorldBossSchedule);
            }

            var attack = boss.Attack;
            if (attack == null || !(attack.IntervalSeconds >= 1) || attack.IntervalSeconds > 60
                || attack.MaxCatchUpAttacks < 1 || attack.MaxCatchUpAttacks > 100 || !(attack.BossDefense >= 0) || double.IsInfinity(attack.BossDefense)
                || (schedule != null && attack.IntervalSeconds > schedule.DurationMinutes * 60.0))
            {
                errors.Add(V.WorldBossAttack);
            }

            var bands = boss.Bands;
            var bandsOk = bands != null && bands.Count >= 1 && bands.Count <= 20;
            var previous = 0.0;
            foreach (var band in bands ?? new List<WorldBossBandConfig>())
            {
                bandsOk &= band != null && band.DamagePercent > previous && band.DamagePercent <= 100
                           && band.IncomeMinutes >= 0 && band.ShellMinutes >= 0 && band.IncomeMinutes <= 100000 && band.ShellMinutes <= 100000
                           && band.ClothingFragments >= 0 && band.BoatFragments >= 0;
                previous = band?.DamagePercent ?? previous;
            }

            if (!bandsOk)
            {
                errors.Add(V.WorldBossBands);
            }

            var ranking = boss.Ranking;
            if (ranking == null || ranking.DollarsByRank == null || ranking.DollarsByRank.Count > 100 || ranking.DollarsByRank.Any(d => d < 0))
            {
                errors.Add(V.WorldBossRanking);
            }

            var income = boss.ReferenceIncome;
            var incomeOk = income != null && income.Count >= 1 && income.Count <= 2000 && income[0] != null && income[0].Level == 1;
            var level = 0;
            foreach (var row in income ?? new List<WorldBossIncomeConfig>())
            {
                incomeOk &= row != null && row.Level > level && row.CoinsPerHour >= 0 && row.ShellsPerHour >= 0;
                level = row?.Level ?? level;
            }

            if (!incomeOk)
            {
                errors.Add(V.WorldBossIncome);
            }

            return errors;
        }

        /// <summary>clans.json (M24-T49, A-174): membership inside the Fisher's levels, sane prices, a name rule and 1 to 10 rewards.</summary>
        public static List<string> ValidateClans(ClansConfig clans, int fisherMaxLevel)
        {
            var errors = new List<string>();
            if (clans == null)
            {
                errors.Add(V.ConfigFileMissing(GameConfigLoader.ClansFile));
                return errors;
            }

            var m = clans.Membership;
            if (m == null || m.MinFisherLevel < 1 || (fisherMaxLevel > 0 && m.MinFisherLevel > fisherMaxLevel)
                || m.BaseMembers < 1 || m.BaseMembers > 1000 || m.MaxMembers < m.BaseMembers || m.MaxMembers > 1000)
            {
                errors.Add(V.ClansMembership);
            }

            var c = clans.Costs;
            if (c == null || c.FirstCreateDollars < 0 || c.CreateDollars < 0 || c.ExtraSlotFirstDollars < 1 || !(c.ExtraSlotGrowth >= 1) || c.ExtraSlotGrowth > 10)
            {
                errors.Add(V.ClansCosts);
            }

            var n = clans.Name;
            if (n == null || n.MinLength < 1 || n.MaxLength < n.MinLength || n.MaxLength > 40)
            {
                errors.Add(V.ClansName);
            }

            if (!(clans.XpBonus >= 0) || clans.XpBonus > 1)
            {
                errors.Add(V.ClansXpBonus);
            }

            var r = clans.Ranking;
            var rankingOk = r != null && r.UtcOffsetMinutes >= -720 && r.UtcOffsetMinutes <= 840
                            && r.RewardsByRank != null && r.RewardsByRank.Count >= 1 && r.RewardsByRank.Count <= 10;
            foreach (var reward in r?.RewardsByRank ?? new List<ClanRewardConfig>())
            {
                rankingOk &= reward != null && reward.Dollars >= 0 && reward.IncomeMinutes >= 0 && reward.ShellMinutes >= 0
                             && reward.IncomeMinutes <= 100000 && reward.ShellMinutes <= 100000 && reward.ClothingFragments >= 0 && reward.BoatFragments >= 0;
            }

            if (!rankingOk)
            {
                errors.Add(V.ClansRanking);
            }

            return errors;
        }

        /// <summary>
        /// missions.json (M24-T54, A-177): unique ids, known types, the parameters each type needs pointing at things the
        /// config has, sane targets and non-negative rewards.
        /// </summary>
        public static List<string> ValidateMissions(MissionsConfig missions, FishCatalogConfig fish, MapsConfig maps, ProgressionConfig progression,
            RodsConfig rods, EquipmentConfig equipment, WorldBossConfig boss)
        {
            var errors = new List<string>();
            const string file = GameConfigLoader.MissionsFile;
            if (missions == null)
            {
                errors.Add(V.ConfigFileMissing(file));
                return errors;
            }

            if (missions.Missions == null || missions.Missions.Count == 0)
            {
                errors.Add(V.Missing(file, "missions"));
                return errors;
            }

            var species = new HashSet<string>((fish?.Species ?? new List<SpeciesConfig>()).Where(x => x != null).Select(x => x.Id), StringComparer.Ordinal);
            var mapIds = new HashSet<string>((maps?.Maps ?? new List<MapConfig>()).Where(x => x != null).Select(x => x.Id), StringComparer.Ordinal);
            var rarities = new HashSet<string>((progression?.Rarity?.Tiers ?? new List<RarityTierConfig>()).Where(x => x != null).Select(x => x.Id), StringComparer.Ordinal);
            var sizes = new HashSet<string>((progression?.Size?.Categories ?? new List<SizeCategoryConfig>()).Where(x => x != null).Select(x => x.Id), StringComparer.Ordinal);
            var rodIds = new HashSet<string>((rods?.Rods ?? new List<RodConfig>()).Where(x => x != null).Select(x => x.Id), StringComparer.Ordinal);
            var boatIds = new HashSet<string>((equipment?.Boats ?? new List<BoatConfig>()).Where(x => x != null).Select(x => x.Id), StringComparer.Ordinal);
            var baitIds = new HashSet<string>((equipment?.Baits ?? new List<BaitConfig>()).Where(x => x != null).Select(x => x.Id), StringComparer.Ordinal);
            var stars = progression?.Genes?.QualityStars?.Count ?? 5;
            var bands = boss?.Bands?.Count ?? 0;
            var fishMax = progression?.FishLevel?.MaxLevel ?? 100;
            var fisherMax = progression?.Fisher?.MaxLevel ?? 1000;

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var m in missions.Missions)
            {
                if (m == null || string.IsNullOrWhiteSpace(m.Id) || !ids.Add(m.Id))
                {
                    errors.Add(V.DuplicateOrEmptyId(file, "missions", m?.Id));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(m.Type) || !Missions.MissionTypes.All.Contains(m.Type))
                {
                    errors.Add(V.MissionUnknownType(m.Id, m.Type));
                    continue;
                }

                var r = m.Reward;
                if (r == null || r.Coins < 0 || r.Shells < 0 || r.Dollars < 0 || r.Coins + r.Shells + r.Dollars <= 0)
                {
                    errors.Add(V.MissionReward(m.Id));
                }

                var computed = Missions.MissionTypes.ComputedTarget.Contains(m.Type);
                var zeroAll = Missions.MissionTypes.ZeroMeansAll.Contains(m.Type);
                if (!computed && (m.Target < 0 || (m.Target == 0 && !zeroAll)))
                {
                    errors.Add(V.MissionTarget(m.Id));
                }

                bool Need(bool ok, string what)
                {
                    if (!ok)
                    {
                        errors.Add(V.MissionParameter(m.Id, what));
                    }

                    return ok;
                }

                switch (m.Type)
                {
                    case Missions.MissionTypes.CatchSpecies:
                        Need(m.SpeciesId != null && species.Contains(m.SpeciesId), "species_id");
                        break;
                    case Missions.MissionTypes.CatchRarity:
                    case Missions.MissionTypes.RarityAllSpecies:
                    case Missions.MissionTypes.CardumeAllRarityMin:
                        Need(m.Rarity != null && rarities.Contains(m.Rarity), "rarity");
                        break;
                    case Missions.MissionTypes.MarketSell:
                        Need(m.Rarity == null || rarities.Contains(m.Rarity), "rarity");
                        break;
                    case Missions.MissionTypes.CatchSizeCategory:
                        Need(m.SizeCategory != null && sizes.Contains(m.SizeCategory), "size_category");
                        break;
                    case Missions.MissionTypes.CatchGeneStars:
                    case Missions.MissionTypes.CardumeGeneStars:
                        Need(m.Stars >= 1 && m.Stars <= stars, "stars");
                        break;
                    case Missions.MissionTypes.CardumeAllLevel:
                        Need(m.Level >= 1 && m.Level <= fishMax, "level");
                        break;
                    case Missions.MissionTypes.FishLevel:
                        Need(m.Target <= fishMax, "target");
                        break;
                    case Missions.MissionTypes.FisherLevel:
                        Need(m.Target <= fisherMax, "target");
                        break;
                    case Missions.MissionTypes.MapUnlocked:
                    case Missions.MissionTypes.MapAllSpecies:
                        Need(m.MapId != null && mapIds.Contains(m.MapId), "map_id");
                        break;
                    case Missions.MissionTypes.RodAssembled:
                        Need(m.RodId != null && rodIds.Contains(m.RodId), "rod_id");
                        break;
                    case Missions.MissionTypes.BoatAssembled:
                        Need(m.BoatId != null && boatIds.Contains(m.BoatId), "boat_id");
                        break;
                    case Missions.MissionTypes.BaitUsed:
                        Need(m.BaitId != null && baitIds.Contains(m.BaitId), "bait_id");
                        break;
                    case Missions.MissionTypes.BossBand:
                        Need(m.Target <= bands, "target");
                        break;
                    case Missions.MissionTypes.MarketBuy:
                        Need(m.GoodsKind == null || m.GoodsKind == "fish", "goods_kind");
                        break;
                    case Missions.MissionTypes.ClothingAssembled:
                    case Missions.MissionTypes.ClothingMaxLevel:
                        Need(m.Target <= (equipment?.Clothing?.Pieces?.Count ?? 0), "target");
                        break;
                    case Missions.MissionTypes.EncyclopediaSpecies:
                        Need(m.Target <= species.Count, "target");
                        break;
                }

                if (m.ReferenceLevel < 0 || m.ReferenceLevel > fisherMax)
                {
                    errors.Add(V.MissionParameter(m.Id, "reference_level"));
                }
            }

            return errors;
        }
    }
}
