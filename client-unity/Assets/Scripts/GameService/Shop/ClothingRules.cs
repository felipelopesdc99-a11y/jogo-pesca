using System;
using System.Collections.Generic;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Persistence;

namespace FishingIdle.GameService.Shop
{
    /// <summary>What the worn clothing adds, by axis (M24-T33, A-164). All zero without clothing.</summary>
    public sealed class ClothingBonuses
    {
        public static readonly ClothingBonuses None = new ClothingBonuses();

        /// <summary>Points on the Catch Success chance (the Luvas).</summary>
        public double CatchSuccess { get; internal set; }

        /// <summary>Added to the rod's rarity efficiency, size quality and Shell yield.</summary>
        public double RarityEfficiency { get; internal set; }
        public double SizeQuality { get; internal set; }
        public double ShellYield { get; internal set; }

        /// <summary>+% Fisher XP on every fish pulled out (the Calça).</summary>
        public double FisherXp { get; internal set; }

        public bool IsNone => CatchSuccess == 0 && RarityEfficiency == 0 && SizeQuality == 0 && ShellYield == 0 && FisherXp == 0;
    }

    /// <summary>A clothing piece that went up a level by being used.</summary>
    public sealed class ClothingLevelView
    {
        public string PieceId { get; internal set; }
        public string PieceName { get; internal set; }
        public int Level { get; internal set; }
    }

    /// <summary>
    /// Clothing (M24-T33, A-164): five pieces (Luvas, Calça, Botas, Chapéu, Camiseta), one of each, assembled with one
    /// kind of fragment, the Fragmento de Roupa, that drops on every map. The player picks which piece to assemble; the
    /// 1st, 2nd … piece cost equipment.json → clothing → fragments_to_assemble in that order. An assembled piece is worn at
    /// once (one slot per piece, all five together) and adds a bonus on its axis; it goes up a level with use: every fish
    /// pulled out (online or offline) gives use_xp_per_fish to every assembled piece, at no cost. Fragments left after
    /// the five pieces are kept (like the boats' extras, OD-060).
    /// </summary>
    public static class ClothingRules
    {
        public static bool Owns(PlayerSave save, string pieceId)
        {
            return pieceId != null && save.Clothing?.Pieces != null && save.Clothing.Pieces.ContainsKey(pieceId);
        }

        /// <summary>The level of an assembled piece, never above the config's maximum (0 when not assembled).</summary>
        public static int Level(GameConfig config, PlayerSave save, string pieceId)
        {
            if (pieceId == null || save.Clothing?.Pieces == null || !save.Clothing.Pieces.TryGetValue(pieceId, out var state) || state == null)
            {
                return 0;
            }

            return Math.Max(1, Math.Min(config.ClothingMaxLevel, state.Level));
        }

        /// <summary>Pieces of the config that are assembled.</summary>
        public static int AssembledCount(GameConfig config, PlayerSave save)
        {
            var count = 0;
            foreach (var piece in config.ClothingPieces)
            {
                if (Owns(save, piece.Id))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>Fragmentos de Roupa the next piece costs; 0 when every piece is assembled or there is no clothing.</summary>
        public static int NextCost(GameConfig config, PlayerSave save)
        {
            var count = AssembledCount(config, save);
            return config.Clothing == null || count >= config.ClothingPieces.Count ? 0 : config.ClothingAssembleCost(count);
        }

        /// <summary>The sum of what the worn pieces add, by axis, at their levels.</summary>
        public static ClothingBonuses Bonuses(GameConfig config, PlayerSave save)
        {
            if (config.Clothing == null || save.Clothing?.Pieces == null || save.Clothing.Pieces.Count == 0)
            {
                return ClothingBonuses.None;
            }

            var total = new ClothingBonuses();
            foreach (var piece in config.ClothingPieces)
            {
                var level = Level(config, save, piece.Id);
                if (level <= 0)
                {
                    continue;
                }

                var bonus = config.ClothingBonusAt(piece, level);
                switch (piece.BonusAxis)
                {
                    case ClothingAxes.CatchSuccess: total.CatchSuccess += bonus; break;
                    case ClothingAxes.RarityEfficiency: total.RarityEfficiency += bonus; break;
                    case ClothingAxes.SizeQuality: total.SizeQuality += bonus; break;
                    case ClothingAxes.ShellYield: total.ShellYield += bonus; break;
                    case ClothingAxes.FisherXp: total.FisherXp += bonus; break;
                }
            }

            return total;
        }

        /// <summary>
        /// The roll after a fish is pulled out (any map): with clothing.fragment_drop_chance, 1 Fragmento de Roupa. Draws
        /// from the cycle's own <see cref="Rng"/> after the catch and the boat and rod fragments, so none of them changes.
        /// Returns whether one dropped.
        /// </summary>
        internal static bool RollOnCatch(GameConfig config, PlayerSave save, Rng rng, bool offline)
        {
            var clothing = config.Clothing;
            if (clothing == null || clothing.FragmentDropChance <= 0 || (offline && !clothing.DropOnOfflineFishing))
            {
                return false;
            }

            if (rng.NextDouble() >= clothing.FragmentDropChance)
            {
                return false;
            }

            AddFragments(save, 1);
            return true;
        }

        public static void AddFragments(PlayerSave save, long amount)
        {
            if (amount <= 0)
            {
                return;
            }

            save.Clothing = save.Clothing ?? new ClothingState();
            var have = save.Clothing.Fragments;
            save.Clothing.Fragments = amount > long.MaxValue - have ? long.MaxValue : have + amount;
        }

        /// <summary>Why a piece cannot be assembled now; None when it can.</summary>
        public static ServiceError AssembleBlocker(GameConfig config, PlayerSave save, string pieceId)
        {
            if (!config.TryGetClothingPiece(pieceId, out _)) return ServiceError.ClothingNotFound;
            if (Owns(save, pieceId)) return ServiceError.ClothingAlreadyOwned;
            if ((save.Clothing?.Fragments ?? 0) < NextCost(config, save)) return ServiceError.NotEnoughClothingFragments;
            return ServiceError.None;
        }

        /// <summary>Assembles a piece (checked by <see cref="AssembleBlocker"/> first): spends the fragments; it is worn at Nv.1.</summary>
        internal static void Assemble(GameConfig config, PlayerSave save, string pieceId, long nowMs)
        {
            var cost = NextCost(config, save);
            save.Clothing.Fragments -= cost;
            save.Clothing.Pieces[pieceId] = new ClothingPieceState { Level = 1, Xp = 0, AssembledAtMs = nowMs };
        }

        /// <summary>
        /// Use XP for every assembled piece (a fish pulled out): each one goes up while its XP reaches the next level,
        /// up to the max level (then the XP stops counting). Levels reached are added to <paramref name="levels"/>.
        /// </summary>
        internal static void GainUse(GameConfig config, PlayerSave save, long xp, List<ClothingLevelView> levels)
        {
            if (xp <= 0 || config.Clothing == null || save.Clothing?.Pieces == null || save.Clothing.Pieces.Count == 0)
            {
                return;
            }

            var max = config.ClothingMaxLevel;
            foreach (var piece in config.ClothingPieces)
            {
                if (!save.Clothing.Pieces.TryGetValue(piece.Id, out var state) || state == null)
                {
                    continue;
                }

                if (state.Level >= max)
                {
                    state.Xp = 0;
                    continue;
                }

                state.Xp = xp > long.MaxValue - state.Xp ? long.MaxValue : state.Xp + xp;
                while (state.Level < max)
                {
                    var need = config.ClothingXpToNextLevel(state.Level);
                    if (need <= 0 || state.Xp < need)
                    {
                        break;
                    }

                    state.Xp -= need;
                    state.Level++;
                    levels?.Add(new ClothingLevelView { PieceId = piece.Id, PieceName = piece.DisplayName, Level = state.Level });
                }

                if (state.Level >= max)
                {
                    state.Xp = 0;
                }
            }
        }
    }
}
