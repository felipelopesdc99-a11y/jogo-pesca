using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.GameService.Aquarium;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Persistence;
using FishingIdle.GameService.Shop;

namespace FishingIdle.GameService.Profile
{
    /// <summary>The Cardume: one per player, 1–6 Aquarium fish in fixed positions.</summary>
    public interface ICardumeService
    {
        CardumeView GetCardume();

        /// <summary>Puts an Aquarium fish in a position (1–6). A fish already elsewhere in the Cardume moves.</summary>
        ServiceResult<CardumeView> SetSlot(int position, long fishId);

        ServiceResult<CardumeView> ClearSlot(int position);
    }

    /// <summary>The player's own Profile: equipment, Inventory, Encyclopedia and records.</summary>
    public interface IProfileService
    {
        ProfileView GetProfile();

        /// <summary>Equips a rod from the Inventory in the only equipment slot.</summary>
        ServiceResult<RodItemView> EquipRod(long itemId);

        /// <summary>Buys the next internal level of a rod with Coins (no failure chance, no materials).</summary>
        ServiceResult<RodItemView> UpgradeRod(long itemId);

        /// <summary>Sells a rod back to the game for part of what was spent on it.</summary>
        ServiceResult<SaleResult> SellRod(long itemId);

        /// <summary>Removes a rod from the Inventory for nothing.</summary>
        ServiceResult<RodItemView> DestroyRod(long itemId);

        /// <summary>Renames the player (M18-T03). The name is trimmed and checked against player_identity.</summary>
        ServiceResult<string> Rename(string name);

        /// <summary>Picks an avatar from player_identity.avatars (M18-T03).</summary>
        ServiceResult<string> SetAvatar(string avatarId);
    }

    public sealed class LocalCardumeService : ICardumeService
    {
        private readonly GameSession _session;
        private readonly LocalAquariumService _aquarium;

        public LocalCardumeService(GameSession session, LocalAquariumService aquarium)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _aquarium = aquarium ?? throw new ArgumentNullException(nameof(aquarium));
        }

        private PlayerSave Save => _session.Save;
        private GameConfig Config => _session.Config;

        public CardumeView GetCardume()
        {
            var formation = Config.Arena.Formation;
            var filled = Save.CardumeSlots.Count(id => id != 0);
            var bonus = CardumeRules.CompleteBonusActive(Config, filled);
            var view = new CardumeView
            {
                Size = Config.CardumeSize,
                Filled = filled,
                CompleteBonusActive = bonus,
                CompleteBonusSlots = Config.Arena.Cardume.CompleteBonus.RequiresFilledSlots,
                CompleteBonusPercent = Config.Arena.Cardume.CompleteBonus.AttackPercent,
            };

            double totalRaw = 0;
            for (var i = 0; i < Save.CardumeSlots.Count; i++)
            {
                var position = i + 1;
                var slot = new CardumeSlotView
                {
                    Position = position,
                    IsFront = formation?.FrontPositions == null ? position <= 3 : formation.FrontPositions.Contains(position),
                };

                var fish = Save.Aquarium.FirstOrDefault(f => f.Id == Save.CardumeSlots[i]);
                if (fish != null)
                {
                    slot.Fish = _aquarium.ToView(fish);
                    slot.EffectiveStats = CardumeRules.Effective(Config, slot.Fish.Stats, bonus);
                    var raw = CardumeRules.RawStrength(Config, slot.EffectiveStats);
                    slot.Strength = CardumeRules.Display(Config, raw);
                    totalRaw += raw;
                }

                view.Slots.Add(slot);
            }

            view.Strength = CardumeRules.Display(Config, totalRaw);
            return view;
        }

        public ServiceResult<CardumeView> SetSlot(int position, long fishId)
        {
            if (Save.Expedition.Active)
            {
                return ServiceResult<CardumeView>.Fail(ServiceError.CardumeLocked);
            }

            if (position < 1 || position > Save.CardumeSlots.Count)
            {
                return ServiceResult<CardumeView>.Fail(ServiceError.InvalidCardumePosition);
            }

            if (Save.Aquarium.All(f => f.Id != fishId))
            {
                return ServiceResult<CardumeView>.Fail(ServiceError.FishNotFound);
            }

            // One Cardume, each fish once: placing a fish that is already in it moves it.
            var current = Save.CardumeSlots.IndexOf(fishId);
            if (current >= 0)
            {
                Save.CardumeSlots[current] = 0;
            }

            Save.CardumeSlots[position - 1] = fishId;
            _session.Persist();
            return ServiceResult<CardumeView>.Ok(GetCardume());
        }

        public ServiceResult<CardumeView> ClearSlot(int position)
        {
            if (Save.Expedition.Active)
            {
                return ServiceResult<CardumeView>.Fail(ServiceError.CardumeLocked);
            }

            if (position < 1 || position > Save.CardumeSlots.Count)
            {
                return ServiceResult<CardumeView>.Fail(ServiceError.InvalidCardumePosition);
            }

            Save.CardumeSlots[position - 1] = 0;
            _session.Persist();
            return ServiceResult<CardumeView>.Ok(GetCardume());
        }
    }

    public sealed class LocalProfileService : IProfileService
    {
        private readonly GameSession _session;
        private readonly IFishingService _fishing;
        private readonly ICardumeService _cardume;

        public LocalProfileService(GameSession session, IFishingService fishing, ICardumeService cardume)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _fishing = fishing ?? throw new ArgumentNullException(nameof(fishing));
            _cardume = cardume ?? throw new ArgumentNullException(nameof(cardume));
        }

        private PlayerSave Save => _session.Save;
        private GameConfig Config => _session.Config;

        public ServiceResult<string> Rename(string name)
        {
            var rules = Config.Progression.PlayerIdentity ?? new PlayerIdentityConfig();
            var clean = System.Text.RegularExpressions.Regex.Replace((name ?? string.Empty).Trim(), "\\s+", " ");
            if (clean.Length < rules.NameMin || clean.Length > rules.NameMax || !clean.All(ch => char.IsLetterOrDigit(ch) || ch == ' ' || ch == '.' || ch == '-' || ch == '_'))
            {
                return ServiceResult<string>.Fail(ServiceError.InvalidName);
            }

            Save.PlayerName = clean;
            _session.Persist();
            _session.Log("Player renamed.");
            return ServiceResult<string>.Ok(clean);
        }

        public ServiceResult<string> SetAvatar(string avatarId)
        {
            var avatars = Config.Progression.PlayerIdentity?.Avatars ?? new List<AvatarConfig>();
            if (!avatars.Any(a => a.Id == avatarId))
            {
                return ServiceResult<string>.Fail(ServiceError.AvatarNotFound);
            }

            Save.AvatarId = avatarId;
            _session.Persist();
            return ServiceResult<string>.Ok(avatarId);
        }

        public ProfileView GetProfile()
        {
            Config.TryGetMap(Save.CurrentMapId, out var map);
            var view = new ProfileView
            {
                PlayerName = Save.PlayerName,
                AvatarId = Save.AvatarId,
                FisherLevel = Save.FisherLevel,
                MapName = map?.DisplayName ?? Save.CurrentMapId,
                CardumeStrength = _cardume.GetCardume().Strength,
                Records = Records(),
            };

            foreach (var avatar in Config.Progression.PlayerIdentity?.Avatars ?? new List<AvatarConfig>())
            {
                view.Avatars.Add(new AvatarView { Id = avatar.Id, Name = avatar.DisplayName, Selected = avatar.Id == Save.AvatarId });
            }

            foreach (var item in Save.Inventory.Where(i => i.Kind == InventoryItem.KindRod).OrderBy(i => i.Id))
            {
                var rod = ToRodView(item, map);
                if (rod == null)
                {
                    continue;
                }

                view.Inventory.Add(rod);
                if (rod.IsEquipped)
                {
                    view.EquippedRod = rod;
                }
            }

            var statsMax = new FishStats();
            foreach (var species in Config.FishCatalog.Species)
            {
                view.Encyclopedia.Add(EncyclopediaEntry(species));
                var stats = MiddleStats(species);
                statsMax.Hp = Math.Max(statsMax.Hp, stats.Hp);
                statsMax.Attack = Math.Max(statsMax.Attack, stats.Attack);
                statsMax.Defense = Math.Max(statsMax.Defense, stats.Defense);
                statsMax.Speed = Math.Max(statsMax.Speed, stats.Speed);
            }

            view.EncyclopediaStatsMax = statsMax;
            return view;
        }

        public ServiceResult<RodItemView> EquipRod(long itemId)
        {
            var item = Save.Inventory.FirstOrDefault(i => i.Id == itemId && i.Kind == InventoryItem.KindRod);
            if (item == null || !Config.TryGetRod(item.RodId, out var rod))
            {
                return ServiceResult<RodItemView>.Fail(ServiceError.ItemNotFound);
            }

            // During a trip the rod must also be allowed where the boat is going.
            Config.TryGetMap(Save.CurrentMapId, out var map);
            Config.TryGetMap(Save.Travel.Active ? Save.Travel.ToMapId : null, out var destination);
            if ((map != null && rod.Tier < map.MinimumRodTier) || (destination != null && rod.Tier < destination.MinimumRodTier))
            {
                return ServiceResult<RodItemView>.Fail(ServiceError.RodNotAllowedOnMap);
            }

            // Cycles completed with the old rod are settled with the old rod.
            _fishing.Sync();
            Save.EquippedRodItemId = item.Id;
            _session.Persist();
            _session.Log("Equipped rod item " + item.Id + " (" + item.RodId + ").");
            return ServiceResult<RodItemView>.Ok(ToRodView(item, map));
        }

        public ServiceResult<RodItemView> UpgradeRod(long itemId)
        {
            var item = FindRod(itemId, out var rod);
            if (item == null)
            {
                return ServiceResult<RodItemView>.Fail(ServiceError.ItemNotFound);
            }

            if (!rod.HasInternalLevels)
            {
                return ServiceResult<RodItemView>.Fail(ServiceError.RodHasNoLevels);
            }

            var cost = Config.RodUpgradeCost(rod, item.Level);
            if (cost <= 0)
            {
                return ServiceResult<RodItemView>.Fail(ServiceError.RodAtMaxLevel);
            }

            if (Save.Coins < cost)
            {
                return ServiceResult<RodItemView>.Fail(ServiceError.NotEnoughCoins);
            }

            var shells = Config.RodUpgradeShellCost(rod, item.Level);
            if (Save.Shells < shells)
            {
                return ServiceResult<RodItemView>.Fail(ServiceError.NotEnoughShells);
            }

            // Cycles already completed are settled at the old level.
            _fishing.Sync();
            Save.Coins -= cost;
            Save.Shells -= shells;
            item.Level++;
            item.UpgradeCoinsInvested += cost;
            _session.Persist();
            _session.Log("Upgraded rod item " + item.Id + " to level " + item.Level + " for " + cost + " coins and " + shells + " shells.");
            Config.TryGetMap(Save.CurrentMapId, out var map);
            return ServiceResult<RodItemView>.Ok(ToRodView(item, map));
        }

        public ServiceResult<SaleResult> SellRod(long itemId)
        {
            var check = CheckDisposable(itemId, out var item, out var rod);
            if (check != ServiceError.None)
            {
                return ServiceResult<SaleResult>.Fail(check);
            }

            var value = RodRules.ResaleValue(rod, item);
            Save.Inventory.Remove(item);
            Save.Coins += value;
            _session.Persist();
            _session.Log("Sold rod item " + item.Id + " for " + value + " coins.");
            return ServiceResult<SaleResult>.Ok(new SaleResult { Count = 1, CoinsGained = value, NewBalance = Save.Coins });
        }

        public ServiceResult<RodItemView> DestroyRod(long itemId)
        {
            var check = CheckDisposable(itemId, out var item, out _);
            if (check != ServiceError.None)
            {
                return ServiceResult<RodItemView>.Fail(check);
            }

            Config.TryGetMap(Save.CurrentMapId, out var map);
            var view = ToRodView(item, map);
            Save.Inventory.Remove(item);
            _session.Persist();
            _session.Log("Destroyed rod item " + item.Id + ".");
            return ServiceResult<RodItemView>.Ok(view);
        }

        private InventoryItem FindRod(long itemId, out RodConfig rod)
        {
            rod = null;
            var item = Save.Inventory.FirstOrDefault(i => i.Id == itemId && i.Kind == InventoryItem.KindRod);
            return item != null && Config.TryGetRod(item.RodId, out rod) ? item : null;
        }

        private ServiceError CheckDisposable(long itemId, out InventoryItem item, out RodConfig rod)
        {
            item = FindRod(itemId, out rod);
            if (item == null) return ServiceError.ItemNotFound;
            if (item.Id == Save.EquippedRodItemId) return ServiceError.RodEquipped;
            if (!RodRules.CanDispose(Config, rod)) return ServiceError.RodNotSellable;
            return ServiceError.None;
        }

        private RodItemView ToRodView(InventoryItem item, MapConfig map)
        {
            if (!Config.TryGetRod(item.RodId, out var rod))
            {
                return null;
            }

            var bonuses = Config.RodBonusesAt(rod, item.Level);
            return new RodItemView
            {
                ItemId = item.Id,
                RodId = rod.Id,
                Name = rod.DisplayName,
                Tier = rod.Tier,
                HasLevels = rod.HasInternalLevels,
                Level = item.Level,
                MaxLevel = Config.RodMaxLevel(rod),
                RarityBonus = bonuses.RarityEfficiency,
                SizeBonus = bonuses.SizeQuality,
                ShellBonus = bonuses.ShellYield,
                CanCatchRare = rod.CanCatchRarities != null && rod.CanCatchRarities.Any(r => Config.RarityRank(r) > 0),
                CanCatchEpic = rod.CanCatchRarities != null && rod.CanCatchRarities.Any(r => Config.RarityRank(r) > 1),
                CanCatchLegendary = rod.CanCatchRarities != null && rod.CanCatchRarities.Any(r => Config.RarityRank(r) > 2),
                CanCatchMythic = rod.CanCatchRarities != null && rod.CanCatchRarities.Any(r => Config.RarityRank(r) > 3),
                GeneratesShells = rod.GeneratesShells,
                IsEquipped = item.Id == Save.EquippedRodItemId,
                AllowedOnCurrentMap = map == null || rod.Tier >= map.MinimumRodTier,
                NextUpgradeCost = rod.HasInternalLevels ? Config.RodUpgradeCost(rod, item.Level) : 0,
                NextUpgradeShells = rod.HasInternalLevels ? Config.RodUpgradeShellCost(rod, item.Level) : 0,
                ResaleValue = RodRules.ResaleValue(rod, item),
                CanDispose = item.Id != Save.EquippedRodItemId && RodRules.CanDispose(Config, rod),
            };
        }

        private EncyclopediaEntryView EncyclopediaEntry(SpeciesConfig species)
        {
            // Map and rarity are shown even before discovery (M22-T11); the name, sizes and counts are not.
            Config.TryGetMap(species.PrimaryMapId, out var map);
            Config.TryGetRarity(species.Rarity, out var rarity);
            var entry = new EncyclopediaEntryView
            {
                SpeciesId = species.Id,
                MapId = map?.Id,
                MapName = map?.DisplayName,
                RarityId = species.Rarity,
                RarityName = rarity?.DisplayName ?? species.Rarity,
            };
            if (!Save.SpeciesRecords.TryGetValue(species.Id, out var record))
            {
                return entry;
            }

            if (map != null)
            {
                var total = map.FishPool.Sum(p => p.CatchWeight);
                var own = map.FishPool.Where(p => p.SpeciesId == species.Id).Sum(p => p.CatchWeight);
                entry.BiteShare = total > 0 ? own / total : 0;
            }

            entry.Discovered = true;
            entry.Name = species.DisplayName;
            entry.LargestCm = record.LargestMm / 10.0;
            entry.SpeciesMaxCm = species.SizeCm.Max;
            entry.SpeciesMinCm = species.SizeCm.Min;
            entry.TimesCaught = record.TimesCaught;
            entry.FirstCaughtAtMs = record.FirstCaughtAtMs;
            entry.BaseStats = MiddleStats(species);
            var category = RecordCategory(species, record.LargestMm);
            if (category != null)
            {
                entry.LargestSizeCategoryId = category.Id;
                entry.LargestSizeCategoryName = category.DisplayName ?? category.Id;
                entry.LargestIsSpecial = category.Special;
            }

            return entry;
        }

        /// <summary>Attributes at level 1 and the middle of the species' size range (the size factor is 1 there).</summary>
        private FishStats MiddleStats(SpeciesConfig species)
        {
            var middleMm = (int)Math.Round((species.SizeCm.Min + species.SizeCm.Max) / 2.0 * 10.0, MidpointRounding.AwayFromZero);
            return FishRules.Stats(Config, species, Math.Max(1, middleMm), 1);
        }

        /// <summary>
        /// The size category a recorded size came from. A catch's size is drawn inside its category's percentile
        /// band and rounded to the millimetre, so the lowest band that could give this size (±0,5 mm) is taken.
        /// </summary>
        private SizeCategoryConfig RecordCategory(SpeciesConfig species, int sizeMm)
        {
            var categories = Config.SizeCategories;
            if (categories == null || categories.Count == 0)
            {
                return null;
            }

            var span = species.SizeCm.Max - species.SizeCm.Min;
            var cm = sizeMm / 10.0;
            var ordered = categories.OrderBy(c => c.PercentileMin).ToList();
            foreach (var c in ordered)
            {
                var low = species.SizeCm.Min + c.PercentileMin * span - 0.05;
                var high = species.SizeCm.Min + c.PercentileMax * span + 0.05;
                if (cm >= low && cm <= high)
                {
                    return c;
                }
            }

            return CatchRules.Percentile(species, sizeMm) < 0.5 ? ordered[0] : ordered[ordered.Count - 1];
        }

        private RecordsView Records()
        {
            var records = new RecordsView
            {
                TotalCatches = Save.Stats.TotalCatches,
                SpeciesDiscovered = Save.SpeciesRecords.Count,
                SpeciesTotal = Config.FishCatalog.Species.Count,
                HighestFishLevel = Save.Aquarium.Count == 0 ? 0 : Save.Aquarium.Max(f => f.Level),
                ExceptionalCatches = Save.Stats.ExceptionalCatches,
                PerfectCatches = Save.Stats.PerfectCatches,
                RareCatches = Save.Stats.RareCatches,
                FishSold = Save.Stats.FishSold,
                CoinsFromSales = Save.Stats.CoinsFromSales,
            };

            // Per rarity: how many species exist, how many were found, and how many fish were caught.
            foreach (var tier in Config.Progression.Rarity.Tiers)
            {
                var species = Config.FishCatalog.Species.Where(sp => sp.Rarity == tier.Id).ToList();
                if (species.Count == 0)
                {
                    continue;
                }

                var tally = new RarityTallyView { RarityId = tier.Id, RarityName = tier.DisplayName, SpeciesTotal = species.Count };
                foreach (var sp in species)
                {
                    if (Save.SpeciesRecords.TryGetValue(sp.Id, out var rec))
                    {
                        tally.SpeciesFound++;
                        tally.Caught += rec.TimesCaught;
                    }
                }

                records.ByRarity.Add(tally);
            }

            var biggest = Save.SpeciesRecords.OrderByDescending(r => r.Value.LargestMm).FirstOrDefault();
            if (biggest.Key != null)
            {
                records.BiggestSpeciesId = biggest.Key;
                records.BiggestSpeciesName = Config.TryGetSpecies(biggest.Key, out var s) ? s.DisplayName : biggest.Key;
                records.BiggestCm = biggest.Value.LargestMm / 10.0;
            }

            return records;
        }
    }
}
