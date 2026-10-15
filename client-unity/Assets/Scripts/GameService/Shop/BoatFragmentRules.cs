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

        /// <summary>Whether the player owns the boat (assembled before; extras then).</summary>
        public bool Owned { get; internal set; }

        /// <summary>
        /// Whether this gain completed the fragments the boat asks for (M24-T58, A-180): nothing is assembled by itself,
        /// the player assembles it with the "Montar" button (Loja → Barcos).
        /// </summary>
        public bool Ready { get; internal set; }

        /// <summary>The boat the player still needs to assemble this one (the one before it), or null when they have it.</summary>
        public string NeedsBoatName { get; internal set; }
    }

    /// <summary>
    /// Boats by fragments (M24-T16, A-157): since M24-T47 (A-173) every fish pulled out, on any map, may drop one fragment
    /// of each boat (one roll per boat, with its own fragment_drop_chance). Since M24-T58 (A-180) the player assembles the
    /// boat with the "Montar" button: it takes fragments_required fragments AND the boat before it (boat 1 asks for the
    /// starter boat, which every player has). Assembling spends only fragments_required; the boat is owned at Nv.1 and is
    /// not put in use by itself. Fragments keep adding up without a limit; the ones above fragments_required stay kept
    /// (and those of an owned boat are extras, OD-060).
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
        /// The rolls after a fish is pulled out (any map, M24-T47): one per boat but the starter one, in tier order, each
        /// with that boat's fragment_drop_chance of 1 fragment of it. Draws from the cycle's own <see cref="Rng"/> after the
        /// catch, so the catch itself never changes; a boat with chance 0 draws nothing. Offline catches drop only when the
        /// config allows it (equipment.json → boat_fragments). Returns the gains (empty when none dropped).
        /// </summary>
        internal static List<FragmentGainView> RollOnCatch(GameConfig config, PlayerSave save, Rng rng, bool offline)
        {
            var gains = new List<FragmentGainView>();
            if (offline && !config.FragmentsDropOffline)
            {
                return gains;
            }

            foreach (var boat in config.FragmentBoats)
            {
                if (boat.FragmentDropChance > 0 && rng.NextDouble() < boat.FragmentDropChance)
                {
                    var gain = Add(config, save, boat, 1);
                    if (gain != null)
                    {
                        gains.Add(gain);
                    }
                }
            }

            return gains;
        }

        /// <summary>
        /// Adds fragments of a boat (the starter boat has none). Nothing is assembled here (M24-T58): the gain says when
        /// the fragments became enough (<see cref="FragmentGainView.Ready"/>) and which boat is still missing before it.
        /// </summary>
        public static FragmentGainView Add(GameConfig config, PlayerSave save, BoatConfig boat, long amount)
        {
            if (boat == null || amount <= 0 || boat.Id == config.StarterBoat.Id)
            {
                return null;
            }

            save.BoatFragments = save.BoatFragments ?? new Dictionary<string, long>();
            var before = Have(save, boat.Id);
            var have = amount > long.MaxValue - before ? long.MaxValue : before + amount;
            save.BoatFragments[boat.Id] = have;
            var required = System.Math.Max(1, boat.FragmentsRequired);
            var gain = new FragmentGainView
            {
                BoatId = boat.Id,
                BoatName = boat.DisplayName,
                Amount = amount,
                Required = boat.FragmentsRequired,
            };
            gain.Have = have;
            gain.Owned = Owns(config, save, boat);
            gain.Ready = !gain.Owned && before < required && have >= required;
            var previous = Previous(config, boat);
            gain.NeedsBoatName = previous != null && !Owns(config, save, previous) ? previous.DisplayName : null;
            return gain;
        }

        /// <summary>The boat right before this one in tier order (the starter boat for boat 1); null for the starter boat.</summary>
        public static BoatConfig Previous(GameConfig config, BoatConfig boat)
        {
            return config.Equipment.Boats.Where(b => b.Tier < boat.Tier).OrderByDescending(b => b.Tier).FirstOrDefault();
        }

        /// <summary>Why a boat cannot be assembled now (M24-T58, A-180); None when it can.</summary>
        public static ServiceError AssembleBlocker(GameConfig config, PlayerSave save, BoatConfig boat)
        {
            if (boat == null) return ServiceError.BoatNotFound;
            if (boat.Id == config.StarterBoat.Id) return ServiceError.BoatAlreadyOwned;
            if (Owns(config, save, boat)) return ServiceError.BoatAlreadyOwned;
            var previous = Previous(config, boat);
            if (previous != null && !Owns(config, save, previous)) return ServiceError.BoatNeedsPrevious;
            if (Have(save, boat.Id) < System.Math.Max(1, boat.FragmentsRequired)) return ServiceError.NotEnoughBoatFragments;
            return ServiceError.None;
        }

        /// <summary>
        /// Assembles a boat (checked by <see cref="AssembleBlocker"/> first): spends fragments_required (the rest stays
        /// kept) and adds the boat at Nv.1. It is not put in use: the player chooses with "Usar" (M24-T58, A-180).
        /// </summary>
        internal static void Assemble(GameConfig config, PlayerSave save, BoatConfig boat)
        {
            save.BoatFragments[boat.Id] = Have(save, boat.Id) - System.Math.Max(1, boat.FragmentsRequired);
            save.OwnedBoatIds = save.OwnedBoatIds ?? new List<string>();
            // A boat missing from BoatLevels is at Nv.1 (GearRules.BoatLevel).
            save.OwnedBoatIds.Add(boat.Id);
        }

        /// <summary>
        /// Only for converting a save from the old boat model (BoatModelMigration): the boats the player had become
        /// fragments of today's boats and are assembled again at once, as before, so a migrated save keeps what it had.
        /// Not used by the game since M24-T58 (A-180): there the player assembles with "Montar".
        /// </summary>
        internal static bool AssembleForMigration(GameConfig config, PlayerSave save, BoatConfig boat)
        {
            return TryAssemble(config, save, boat);
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
