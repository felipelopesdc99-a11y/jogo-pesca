using System.Collections.Generic;
using System.Linq;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Persistence;

namespace FishingIdle.GameService.Shop
{
    /// <summary>Fragments of one boat received at once (a catch, a pass reward).</summary>
    public sealed class FragmentGainView
    {
        public string BoatId { get; internal set; }
        public string BoatName { get; internal set; }
        public long Amount { get; internal set; }

        /// <summary>Fragments of this boat after the gain (extras when <see cref="Owned"/>).</summary>
        public long Have { get; internal set; }
        public int Required { get; internal set; }

        /// <summary>Whether the player owns the boat after this gain (assembled now or before).</summary>
        public bool Owned { get; internal set; }

        /// <summary>Whether this gain completed the boat.</summary>
        public bool Assembled { get; internal set; }
    }

    /// <summary>
    /// Boats by fragments (M24-T16, A-157): each boat belongs to one or more maps (M24-T42, A-168: maps 6 to 10 drop boats
    /// 1 to 5); a fish pulled out there may drop one of its
    /// fragments; fragments_required of them assemble the boat, which is then owned at Nv.1 and put in use when it is
    /// better than the boat in use. Fragments that arrive after the boat is owned are kept as extras (OD-060).
    /// </summary>
    public static class BoatFragmentRules
    {
        /// <summary>Fragments of a boat the player has now (extras when the boat is owned).</summary>
        public static long Have(PlayerSave save, string boatId)
        {
            return boatId != null && save.BoatFragments != null && save.BoatFragments.TryGetValue(boatId, out var n) ? n : 0;
        }

        public static bool Owns(GameConfig config, PlayerSave save, BoatConfig boat)
        {
            return boat.Id == config.StarterBoat.Id || (save.OwnedBoatIds != null && save.OwnedBoatIds.Contains(boat.Id));
        }

        /// <summary>
        /// The roll after a fish is pulled out on <paramref name="map"/>: with that map's chance for its boat, 1 fragment of it.
        /// Draws from the cycle's own <see cref="Rng"/> after the catch, so the catch itself never changes. Offline catches
        /// drop only when the config allows it (equipment.json → boat_fragments). Returns the gain, or null.
        /// </summary>
        internal static FragmentGainView RollOnCatch(GameConfig config, PlayerSave save, MapConfig map, Rng rng, bool offline)
        {
            var chance = map == null ? 0.0 : config.BoatFragmentChanceOn(map.Id);
            if (map == null || !config.TryGetBoatForMap(map.Id, out var boat) || chance <= 0)
            {
                return null;
            }

            if (offline && !config.FragmentsDropOffline)
            {
                return null;
            }

            return rng.NextDouble() < chance ? Add(config, save, boat, 1) : null;
        }

        /// <summary>
        /// Adds fragments of a boat and assembles it when they reach fragments_required (the starter boat has none).
        /// Assembling spends the fragments, adds the boat at Nv.1 and puts it in use when its bonus beats the boat in use.
        /// </summary>
        public static FragmentGainView Add(GameConfig config, PlayerSave save, BoatConfig boat, long amount)
        {
            if (boat == null || amount <= 0 || boat.Id == config.StarterBoat.Id)
            {
                return null;
            }

            save.BoatFragments = save.BoatFragments ?? new Dictionary<string, long>();
            var have = Have(save, boat.Id);
            have = amount > long.MaxValue - have ? long.MaxValue : have + amount;
            save.BoatFragments[boat.Id] = have;
            var gain = new FragmentGainView
            {
                BoatId = boat.Id,
                BoatName = boat.DisplayName,
                Amount = amount,
                Required = boat.FragmentsRequired,
            };
            gain.Assembled = TryAssemble(config, save, boat);
            gain.Have = Have(save, boat.Id);
            gain.Owned = Owns(config, save, boat);
            return gain;
        }

        /// <summary>Assembles every boat whose fragments are already enough (a config that lowered fragments_required). Returns their ids.</summary>
        internal static List<string> AssembleReady(GameConfig config, PlayerSave save)
        {
            var assembled = new List<string>();
            foreach (var boat in config.Equipment.Boats.OrderBy(b => b.Tier))
            {
                if (boat.Id != config.StarterBoat.Id && TryAssemble(config, save, boat))
                {
                    assembled.Add(boat.Id);
                }
            }

            return assembled;
        }

        private static bool TryAssemble(GameConfig config, PlayerSave save, BoatConfig boat)
        {
            var required = System.Math.Max(1, boat.FragmentsRequired);
            if (Owns(config, save, boat) || Have(save, boat.Id) < required)
            {
                return false;
            }

            save.BoatFragments[boat.Id] = Have(save, boat.Id) - required;
            save.OwnedBoatIds = save.OwnedBoatIds ?? new List<string>();
            // A boat missing from BoatLevels is at Nv.1 (GearRules.BoatLevel).
            save.OwnedBoatIds.Add(boat.Id);

            // The new boat goes in use only when it is better than the one in use (a player who chose a boat keeps it otherwise).
            var active = GearRules.ActiveBoat(config, save);
            if (config.BoatBonusAt(boat, 1) > GearRules.BoatBonus(config, save, active))
            {
                save.BoatId = boat.Id;
            }

            return true;
        }
    }
}
