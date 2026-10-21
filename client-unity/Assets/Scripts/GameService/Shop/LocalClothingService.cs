using System;
using System.Collections.Generic;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Persistence;

namespace FishingIdle.GameService.Shop
{
    /// <summary>One clothing piece as the Shop's "Roupas" tab shows it (M24-T33, A-164).</summary>
    public sealed class ClothingPieceView
    {
        public string PieceId { get; internal set; }
        public string Name { get; internal set; }
        public string Description { get; internal set; }

        /// <summary>One of ClothingAxes.</summary>
        public string Axis { get; internal set; }
        public bool Owned { get; internal set; }

        /// <summary>The piece's level (0 when not assembled).</summary>
        public int Level { get; internal set; }
        public int MaxLevel { get; internal set; }

        /// <summary>Use XP inside the level and what the next level asks (0 at the max level or not assembled).</summary>
        public long Xp { get; internal set; }
        public long XpToNext { get; internal set; }

        /// <summary>The bonus now (Nv.1 when not assembled), one level up, at Nv.1 and at the max level.</summary>
        public double Bonus { get; internal set; }
        public double BonusNextLevel { get; internal set; }
        public double BonusAtLevel1 { get; internal set; }
        public double BonusAtMax { get; internal set; }

        /// <summary>Why it cannot be assembled now; None when it can (ClothingAlreadyOwned once assembled).</summary>
        public ServiceError AssembleBlocker { get; internal set; }
    }

    public sealed class ClothingView
    {
        /// <summary>Fragmentos de Roupa the player has (extras once the five pieces are assembled).</summary>
        public long Fragments { get; internal set; }

        /// <summary>Fragments the next piece costs, whichever it is; 0 when all are assembled.</summary>
        public int NextCost { get; internal set; }

        /// <summary>True when the fragments pay for the next piece and a piece is still missing.</summary>
        public bool CanAssemble { get; internal set; }
        public int Assembled { get; internal set; }
        public double FragmentChance { get; internal set; }
        public ClothingBonuses Bonuses { get; internal set; }
        public List<ClothingPieceView> Pieces { get; } = new List<ClothingPieceView>();
    }

    /// <summary>Clothing (M24-T33, A-164): the pieces, the Fragmentos de Roupa and assembling the piece the player picks.</summary>
    public interface IClothingService
    {
        ClothingView GetClothing();

        /// <summary>Assembles a piece with the Fragmentos de Roupa the next piece costs; it is worn at once, at Nv.1.</summary>
        ServiceResult<ClothingView> Assemble(string pieceId);
    }

    public sealed class LocalClothingService : IClothingService
    {
        private readonly GameSession _session;
        private readonly IFishingService _fishing;

        public LocalClothingService(GameSession session, IFishingService fishing)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _fishing = fishing ?? throw new ArgumentNullException(nameof(fishing));
        }

        private PlayerSave Save => _session.Save;
        private GameConfig Config => _session.Config;

        public ClothingView GetClothing()
        {
            var config = Config;
            var save = Save;
            var view = new ClothingView
            {
                Fragments = save.Clothing?.Fragments ?? 0,
                NextCost = ClothingRules.NextCost(config, save),
                CanAssemble = ClothingRules.NextCost(config, save) > 0 && (save.Clothing?.Fragments ?? 0) >= ClothingRules.NextCost(config, save),
                Assembled = ClothingRules.AssembledCount(config, save),
                FragmentChance = config.Clothing?.FragmentDropChance ?? 0,
                Bonuses = ClothingRules.Bonuses(config, save),
            };

            var max = config.ClothingMaxLevel;
            foreach (var piece in config.ClothingPieces)
            {
                var level = ClothingRules.Level(config, save, piece.Id);
                var owned = level > 0;
                var state = owned ? save.Clothing.Pieces[piece.Id] : null;
                var now = Math.Max(1, level);
                view.Pieces.Add(new ClothingPieceView
                {
                    PieceId = piece.Id,
                    Name = piece.DisplayName,
                    Description = piece.Description,
                    Axis = piece.BonusAxis,
                    Owned = owned,
                    Level = level,
                    MaxLevel = max,
                    Xp = owned && level < max ? state.Xp : 0,
                    XpToNext = owned ? config.ClothingXpToNextLevel(level) : 0,
                    Bonus = config.ClothingBonusAt(piece, now),
                    BonusNextLevel = config.ClothingBonusAt(piece, Math.Min(max, now + 1)),
                    BonusAtLevel1 = config.ClothingBonusAt(piece, 1),
                    BonusAtMax = config.ClothingBonusAt(piece, max),
                    AssembleBlocker = ClothingRules.AssembleBlocker(config, save, piece.Id),
                });
            }

            return view;
        }

        public ServiceResult<ClothingView> Assemble(string pieceId)
        {
            var blocker = ClothingRules.AssembleBlocker(Config, Save, pieceId);
            if (blocker != ServiceError.None)
            {
                return ServiceResult<ClothingView>.Fail(blocker);
            }

            // Cycles completed before the piece is worn are settled without it.
            _fishing.Sync();
            var cost = ClothingRules.NextCost(Config, Save);
            ClothingRules.Assemble(Config, Save, pieceId, _session.Clock.UtcNowMs);
            _session.Persist();
            _session.Log("Assembled clothing piece " + pieceId + " for " + cost + " clothing fragments.");
            return ServiceResult<ClothingView>.Ok(GetClothing());
        }
    }
}
