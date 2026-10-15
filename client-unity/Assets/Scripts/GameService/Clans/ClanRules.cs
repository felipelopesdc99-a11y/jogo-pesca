using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Persistence;
using FishingIdle.GameService.Profile;

namespace FishingIdle.GameService.Clans
{
    /// <summary>
    /// Pure rules of the Clãs (M24-T49, A-174, TD-055): who may be in a clan, places and their price, the name, the +1% XP,
    /// a fish's Strength (the Cardume's own rule, CardumeRules), the clan's Strength and the daily ranking at 00:00 in the
    /// ranking's fixed time zone. No clock: the service passes "now".
    /// </summary>
    public static class ClanRules
    {
        public const long DayMs = 86_400_000L;

        // ------------------------------------------------------------------ membership

        /// <summary>The clan the player is in (MyClanId, with the player among its members), or null.</summary>
        public static ClanRecord MyClan(PlayerSave save)
        {
            var state = save?.Clans;
            if (state == null || string.IsNullOrEmpty(state.MyClanId) || state.KnownClans == null)
            {
                return null;
            }

            var clan = state.KnownClans.FirstOrDefault(c => c != null && c.Id == state.MyClanId);
            return clan != null && MyMember(save, clan) != null ? clan : null;
        }

        /// <summary>The player's own line in a clan, or null.</summary>
        public static ClanMemberRecord MyMember(PlayerSave save, ClanRecord clan)
        {
            return clan?.Members?.FirstOrDefault(m => m != null && m.PlayerId == save.PlayerId);
        }

        /// <summary>The Fisher level reaches the clans' minimum (Nv.100).</summary>
        public static bool LevelAllows(GameConfig config, PlayerSave save) => save.FisherLevel >= config.Clans.Membership.MinFisherLevel;

        /// <summary>
        /// Dólares to create a clan now: each player's first clan costs costs.first_create_dollars (free), every later one
        /// costs.create_dollars, even after deleting the first (owner, 13/10/2026). Saves from before the counter existed
        /// count the clans already numbered.
        /// </summary>
        public static long CreatePrice(GameConfig config, PlayerSave save)
        {
            var state = save.Clans;
            var created = state == null ? 0 : Math.Max(state.ClansCreated, state.NextClanNumber - 1);
            return created > 0 ? config.Clans.Costs.CreateDollars : config.Clans.Costs.FirstCreateDollars;
        }

        /// <summary>Places of a clan: the base plus the ones bought, never above max_members.</summary>
        public static int Capacity(GameConfig config, ClanRecord clan)
        {
            var m = config.Clans.Membership;
            var extra = Math.Max(0, clan?.ExtraSlots ?? 0);
            return (int)Math.Min(m.MaxMembers, (long)m.BaseMembers + extra);
        }

        /// <summary>Dólares of the next extra place when <paramref name="extraBought"/> were bought: first × growth^bought, rounded.</summary>
        public static long SlotPrice(GameConfig config, int extraBought)
        {
            var c = config.Clans.Costs;
            var value = Math.Round(c.ExtraSlotFirstDollars * Math.Pow(c.ExtraSlotGrowth, Math.Max(0, extraBought)), MidpointRounding.AwayFromZero);
            return value >= long.MaxValue || double.IsInfinity(value) || double.IsNaN(value) ? long.MaxValue : Math.Max(1L, (long)value);
        }

        /// <summary>The name trimmed, with repeated spaces turned into one.</summary>
        public static string CleanName(string name)
        {
            return System.Text.RegularExpressions.Regex.Replace((name ?? string.Empty).Trim(), "\\s+", " ");
        }

        /// <summary>Length inside clans.json → name and only letters, digits, space, dot, hyphen and underscore (like the player name).</summary>
        public static bool ValidName(GameConfig config, string clean)
        {
            var rule = config.Clans.Name;
            return clean != null && clean.Length >= rule.MinLength && clean.Length <= rule.MaxLength
                   && clean.All(ch => char.IsLetterOrDigit(ch) || ch == ' ' || ch == '.' || ch == '-' || ch == '_');
        }

        /// <summary>Another known clan already has this name (upper and lower case count as the same).</summary>
        public static bool NameTaken(PlayerSave save, string clean)
        {
            return save.Clans.KnownClans.Any(c => c != null && string.Equals(CleanName(c.Name), clean, StringComparison.OrdinalIgnoreCase));
        }

        // ------------------------------------------------------------------ XP and Strength

        /// <summary>The player gets the clan's XP bonus now: in a clan, at the level clans ask, with a bonus above 0.</summary>
        public static bool BonusActive(GameConfig config, PlayerSave save)
        {
            return (config.Clans?.XpBonus ?? 0) > 0 && LevelAllows(config, save) && MyClan(save) != null;
        }

        /// <summary>
        /// The clan's extra Fisher XP for a fish worth <paramref name="fishXp"/>: +xp_bonus of it, only in a clan. The
        /// fraction of a point is kept (ClanState.XpCarry) and added to the next fish, so the bonus is exactly +1% over time
        /// even on fish worth less than 100 XP (rounding each fish would give them 0).
        /// </summary>
        public static long TakeBonusXp(GameConfig config, PlayerSave save, long fishXp)
        {
            if (fishXp <= 0 || !BonusActive(config, save))
            {
                return 0;
            }

            var carry = save.Clans.XpCarry;
            var exact = fishXp * config.Clans.XpBonus + (carry > 0 && carry < 1 ? carry : 0);
            if (double.IsNaN(exact) || double.IsInfinity(exact) || exact >= long.MaxValue)
            {
                save.Clans.XpCarry = 0;
                return long.MaxValue;
            }

            var whole = Math.Floor(exact);
            save.Clans.XpCarry = exact - whole;
            return (long)whole;
        }

        /// <summary>
        /// A fish's Strength: the Cardume's rule (CardumeRules.RawStrength, shown with CardumeRules.Display) on its own
        /// attributes, without the complete-Cardume bonus (it is one fish, not a Cardume). 0 for an unknown species.
        /// </summary>
        public static long FishStrength(GameConfig config, FishInstance fish)
        {
            if (fish == null || !config.TryGetSpecies(fish.SpeciesId, out var species))
            {
                return 0;
            }

            var stats = FishRules.Stats(config, species, fish.SizeMm, fish.Level, fish.SizeCategoryId, GeneRules.IsValid(config, fish.Genes) ? fish.Genes : GeneRules.Neutral(config));
            return Math.Max(0L, CardumeRules.Display(config, CardumeRules.RawStrength(config, stats)));
        }

        /// <summary>The player's fish in the clan's Cardume, or null (none, or no longer in the Aquarium).</summary>
        public static FishInstance MyFish(PlayerSave save, ClanMemberRecord me)
        {
            return me == null || me.FishId <= 0 ? null : save.Aquarium.FirstOrDefault(f => f != null && f.Id == me.FishId);
        }

        /// <summary>A member's share of the clan's Strength: the player's from the Aquarium, the others' as reported.</summary>
        public static long MemberStrength(GameConfig config, PlayerSave save, ClanMemberRecord member)
        {
            if (member == null)
            {
                return 0;
            }

            return member.PlayerId == save.PlayerId ? FishStrength(config, MyFish(save, member)) : Math.Max(0L, member.FishStrength);
        }

        /// <summary>The clan's Strength: the sum of its members' fish (SafeMath: never wraps).</summary>
        public static long ClanStrength(GameConfig config, PlayerSave save, ClanRecord clan)
        {
            long total = 0;
            foreach (var member in clan?.Members ?? new List<ClanMemberRecord>())
            {
                total = SafeMath.Add(total, MemberStrength(config, save, member));
            }

            return total;
        }

        /// <summary>The player's fish left the Aquarium (sold, eaten, listed): it leaves the clan's Cardume. True when it changed.</summary>
        public static bool DropMissingFish(PlayerSave save)
        {
            var me = MyMember(save, MyClan(save));
            if (me == null || me.FishId <= 0 || MyFish(save, me) != null)
            {
                return false;
            }

            me.FishId = 0;
            return true;
        }

        /// <summary>
        /// Keeps the clan state usable after loading: a MyClanId that points nowhere (or to a clan without the player) is
        /// cleared, a clan nobody is in any more is dropped, the player's missing fish leaves. True when it changed.
        /// </summary>
        public static bool Repair(PlayerSave save)
        {
            var state = save.Clans;
            var changed = state.KnownClans.RemoveAll(c => c.Members.Count == 0 || string.IsNullOrWhiteSpace(c.Id)) > 0;
            if (!string.IsNullOrEmpty(state.MyClanId) && MyClan(save) == null)
            {
                state.MyClanId = null;
                changed = true;
            }

            changed |= DropMissingFish(save);
            foreach (var clan in state.KnownClans)
            {
                clan.ExtraSlots = Math.Max(0, clan.ExtraSlots);
            }

            return changed;
        }

        // ------------------------------------------------------------------ the daily ranking

        private static long OffsetMs(GameConfig config) => config.Clans.Ranking.UtcOffsetMinutes * 60_000L;

        /// <summary>The ranking day of a moment: whole days since 01/01/1970 in the ranking's time zone (Brasília).</summary>
        public static long DayOf(GameConfig config, long utcMs)
        {
            var local = utcMs + OffsetMs(config);
            var day = local / DayMs;
            return local < 0 && local % DayMs != 0 ? day - 1 : day;
        }

        /// <summary>The next 00:00 in the ranking's time zone after <paramref name="utcMs"/>.</summary>
        public static long NextResetMs(GameConfig config, long utcMs) => (DayOf(config, utcMs) + 1) * DayMs - OffsetMs(config);

        /// <summary>The calendar date of a ranking day (for "DD/MM/AAAA").</summary>
        public static DateTime DateOf(long day) => new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Unspecified).AddDays(day);

        /// <summary>
        /// The clans in ranking order: Strength from the highest, ties by the older clan, then the id. A clan with no fish in
        /// its Cardume (Strength 0) is not ranked.
        /// </summary>
        public static List<(ClanRecord Clan, long Strength)> Ranked(GameConfig config, PlayerSave save)
        {
            return save.Clans.KnownClans
                .Select(c => (Clan: c, Strength: ClanStrength(config, save, c)))
                .Where(x => x.Strength > 0)
                .OrderByDescending(x => x.Strength)
                .ThenBy(x => x.Clan.CreatedAtMs)
                .ThenBy(x => x.Clan.Id, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>The reward of a rank (1-based), or null for a rank without one.</summary>
        public static ClanRewardConfig RewardFor(GameConfig config, int rank)
        {
            var list = config.Clans.Ranking.RewardsByRank;
            return rank >= 1 && rank <= list.Count ? list[rank - 1] : null;
        }
    }
}
