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
            EquipmentConfig equipment)
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
                    else if (rod.Acquisition.PurchaseCostCoins < 0 || rod.Acquisition.PurchaseCostShells < 0 || rod.Acquisition.UnlockFisherLevel < 1)
                    {
                        errors.Add(V.NegativeValue(GameConfigLoader.RodsFile, rod.Id + ".acquisition"));
                    }

                    if (rod.HasInternalLevels)
                    {
                        var levelCount = rod.BonusesPerLevel?.RarityEfficiency?.Count ?? 0;
                        for (var level = 2; level <= levelCount; level++)
                        {
                            var step = rod.UpgradeCosts?.FirstOrDefault(c => c.ToLevel == level);
                            if (step == null || step.CostCoins <= 0 || step.CostShells < 0)
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

            // ---- maps × rods: every rod allowed on a map must have something to catch there, or fishing
            // would have no species to draw from (the game would stop with an error).
            if (maps.Maps != null && rods.Rods != null)
            {
                foreach (var map in maps.Maps.Where(m => m.FishPool != null))
                {
                    foreach (var rod in rods.Rods.Where(r => r.Tier >= map.MinimumRodTier && r.CanCatchRarities != null))
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
                    || sup.MaxFishLevel < 1 || sup.PriceRatio.Min <= 0 || sup.PriceRatio.Max < sup.PriceRatio.Min)
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

                    if (boat.CostCoins < 0 || boat.CostShells < 0)
                    {
                        errors.Add(V.NegativeValue(GameConfigLoader.EquipmentFile, boat.Id + " (custo)"));
                    }
                }

                var starter = equipment.Boats.OrderBy(b => b.Tier).First();
                if (starter.CostCoins != 0 || starter.CostShells != 0)
                {
                    errors.Add(V.StarterBoatNotFree(starter.Id));
                }
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
            }

            return errors;
        }
    }
}
