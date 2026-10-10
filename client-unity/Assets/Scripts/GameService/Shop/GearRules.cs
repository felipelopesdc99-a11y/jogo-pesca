using FishingIdle.GameService.Config;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Persistence;

namespace FishingIdle.GameService.Shop
{
    /// <summary>
    /// The boat and bait in use and what they add to the Catch Success chance
    /// (docs/SISTEMA_SUCESSO_PESCA.md). The rod's own bonus comes from its level (GameConfig.RodBonusesAt).
    /// </summary>
    public static class GearRules
    {
        /// <summary>
        /// The boat in use; the starter boat when none was chosen, the id is unknown or the boat was
        /// never bought (an edited save cannot sail a boat it does not own).
        /// </summary>
        public static BoatConfig ActiveBoat(GameConfig config, PlayerSave save)
        {
            if (config.TryGetBoat(save.BoatId, out var boat)
                && (boat.Id == config.StarterBoat.Id || (save.OwnedBoatIds != null && save.OwnedBoatIds.Contains(boat.Id))))
            {
                return boat;
            }

            return config.StarterBoat;
        }

        /// <summary>The bait in use while it still has charges, or null.</summary>
        public static BaitConfig ActiveBait(GameConfig config, PlayerSave save)
        {
            return save.ActiveBaitId != null && config.TryGetBait(save.ActiveBaitId, out var bait) && Charges(save, bait.Id) > 0 ? bait : null;
        }

        /// <summary>The level of a boat (M24-T13): what the save says, 1 when it says nothing, never above the boat's maximum.</summary>
        public static int BoatLevel(GameConfig config, PlayerSave save, BoatConfig boat)
        {
            var level = save.BoatLevels != null && save.BoatLevels.TryGetValue(boat.Id, out var l) ? l : 1;
            return System.Math.Max(1, System.Math.Min(config.BoatMaxLevel(boat), level));
        }

        /// <summary>The boat's Catch Success bonus at the level the player took it to.</summary>
        public static double BoatBonus(GameConfig config, PlayerSave save, BoatConfig boat)
        {
            return config.BoatBonusAt(boat, BoatLevel(config, save, boat));
        }

        public static int Charges(PlayerSave save, string baitId)
        {
            return save.BaitCharges != null && baitId != null && save.BaitCharges.TryGetValue(baitId, out var n) ? n : 0;
        }

        /// <summary>Boat + bait bonus right now, without spending anything (what the screens show).</summary>
        public static double Bonus(GameConfig config, PlayerSave save)
        {
            return BoatBonus(config, save, ActiveBoat(config, save)) + (ActiveBait(config, save)?.CatchSuccessBonus ?? 0.0);
        }

        /// <summary>
        /// The boat + bait bonus for one fishing attempt. The bait spends one charge per attempt,
        /// caught or not; when its last charge goes, the update says so.
        /// </summary>
        public static double SpendAttempt(GameConfig config, PlayerSave save, FishingUpdate update)
        {
            var bonus = BoatBonus(config, save, ActiveBoat(config, save));
            var bait = ActiveBait(config, save);
            if (bait == null)
            {
                return bonus;
            }

            var left = Charges(save, bait.Id) - 1;
            save.BaitCharges[bait.Id] = left;
            if (left <= 0)
            {
                update.BaitRanOut = bait.DisplayName;
            }

            return bonus + bait.CatchSuccessBonus;
        }
    }
}
