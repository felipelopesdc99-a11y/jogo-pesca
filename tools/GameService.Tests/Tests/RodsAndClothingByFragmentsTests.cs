using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Pass;
using FishingIdle.GameService.Persistence;
using FishingIdle.GameService.Shop;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>
/// Rods by fragments (M24-T32, A-163) and clothing (M24-T33, A-164): since M24-T47 (A-173) every rod, every boat and the
/// Fragmento de Roupa drop on every map, each in its own roll at 0,1%, so the progression never locks; rods are out of the
/// Shop and the ones a player has stay; any clothing piece can be assembled, each piece adds to its own axis and goes up a
/// level with use; the 95% ceiling holds; the pass gives both; the rolls come after the catch, so the catches never change.
/// </summary>
public sealed class RodsAndClothingByFragmentsTests
{
    private static GameConfig Real => TestSupport.RealConfig();

    /// <summary>The real config with every bite pulled out and an edit of rods.json and equipment.json.</summary>
    private static GameConfig With(Action<JObject, JObject> edit, bool certainCatch = true)
    {
        var texts = TestSupport.ConfigTexts();
        if (certainCatch)
        {
            var progression = JObject.Parse(texts[GameConfigLoader.ProgressionFile]);
            foreach (var tier in progression["rarity"]!["tiers"]!)
            {
                tier["catch_success_base"] = 1.0;
            }

            progression["fishing"]!["catch_success_max"] = 1.0;
            texts[GameConfigLoader.ProgressionFile] = progression.ToString();
        }

        var rods = JObject.Parse(texts[GameConfigLoader.RodsFile]);
        var equipment = JObject.Parse(texts[GameConfigLoader.EquipmentFile]);
        edit(rods, equipment);
        texts[GameConfigLoader.RodsFile] = rods.ToString();
        texts[GameConfigLoader.EquipmentFile] = equipment.ToString();
        var result = GameConfigLoader.LoadFromTexts(texts);
        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        return result.Config;
    }

    private static JToken RodJson(JObject rods, string id) => rods["rods"]!.Single(r => (string)r["id"]! == id);

    /// <summary>No boat, rod or clothing drop except the ones the test sets (rods at TestSupport.NeverDrops, see there).</summary>
    private static void NoDrops(JObject rods, JObject equipment)
    {
        foreach (var rod in rods["rods"]!.Where(r => r["fragment_drop_chance"] != null))
        {
            rod["fragment_drop_chance"] = TestSupport.NeverDrops;
        }

        foreach (var boat in equipment["boats"]!.Where(b => b["fragment_drop_chance"] != null))
        {
            boat["fragment_drop_chance"] = 0.0;
        }

        equipment["clothing"]!["fragment_drop_chance"] = 0.0;
    }

    private static List<FishingUpdate> Cycles(LocalGame game, ManualClock clock, int count)
    {
        var updates = new List<FishingUpdate>();
        for (var i = 0; i < count; i++)
        {
            clock.AdvanceSeconds(game.Session.Config.Fishing.OnlineCycleSeconds);
            updates.Add(game.Fishing.Sync());
        }

        return updates;
    }

    // ------------------------------------------------------------------ every fragment on every map (M24-T47, A-173)

    [Fact]
    public void Every_rod_boat_and_the_clothing_drop_on_every_map_at_0_1_percent()
    {
        var config = Real;
        Assert.True(GameConfig.IsFreeClaimRod(config.StarterRod));
        Assert.Null(typeof(RodConfig).GetProperty("FragmentMapId"));
        Assert.Null(typeof(BoatFragmentMapConfig).GetProperty("FragmentDropChance"));

        // One roll per rod and per boat, in tier order; the starter rod and boat take no fragments.
        Assert.Equal(new[] { "rod_01", "rod_02", "rod_03", "rod_04", "rod_05" }, config.FragmentRods.Select(r => r.Id));
        Assert.Equal(new[] { "boat_01", "boat_02", "boat_03", "boat_04", "boat_05" }, config.FragmentBoats.Select(b => b.Id));
        Assert.All(config.FragmentRods, rod =>
        {
            Assert.True(GameConfig.IsFragmentRod(rod));
            Assert.Equal(0.001, rod.FragmentDropChance, 9);
            Assert.True(rod.FragmentsRequired >= 10);
        });
        Assert.All(config.FragmentBoats, boat => Assert.Equal(0.001, boat.FragmentDropChance, 9));
        Assert.Equal(0.001, config.Clothing.FragmentDropChance, 9);
    }

    [Fact]
    public void The_progression_never_locks_every_map_s_rod_drops_while_fishing()
    {
        var config = Real;
        foreach (var map in config.Maps.Maps.Where(m => m.MinimumRodTier > 0))
        {
            // Every rod drops on every map: a rod of the tier the map asks for (or more) only needs a chance above 0.
            var ok = config.FragmentRods.Any(rod => rod.Tier >= map.MinimumRodTier && rod.FragmentDropChance > 0);
            Assert.True(ok, map.Id + " would lock the progression");
        }
    }

    [Fact]
    public void A_config_that_would_lock_the_progression_is_rejected_in_portuguese()
    {
        // Rod 5 never drops: map 10 asks for it and nobody could ever get there.
        var none = TestSupport.ConfigWith(GameConfigLoader.RodsFile, j =>
        {
            var root = JObject.Parse(j);
            RodJson(root, "rod_05")["fragment_drop_chance"] = 0.0;
            return root.ToString();
        });
        Assert.False(none.Succeeded);
        Assert.Contains(none.Errors, e => e.Contains("map_10") && e.Contains("travaria"));

        var chance = TestSupport.ConfigWith(GameConfigLoader.RodsFile, j =>
        {
            var root = JObject.Parse(j);
            RodJson(root, "rod_03")["fragment_drop_chance"] = 1.5;
            return root.ToString();
        });
        Assert.False(chance.Succeeded);
        Assert.Contains(chance.Errors, e => e.Contains("rod_03.fragment_drop_chance"));

        var sold = TestSupport.ConfigWith(GameConfigLoader.RodsFile, j => j.Replace("\"method\": \"fragments\"", "\"method\": \"coin_purchase\""));
        Assert.False(sold.Succeeded);
        Assert.Contains(sold.Errors, e => e.Contains("acquisition.method"));
    }

    // ------------------------------------------------------------------ rod fragments while fishing

    [Fact]
    public void Rod_fragments_drop_on_every_map_each_rod_in_its_own_roll_and_assemble_the_rod()
    {
        var config = With((rods, equipment) =>
        {
            NoDrops(rods, equipment);
            RodJson(rods, "rod_01")["fragment_drop_chance"] = 1.0;
            RodJson(rods, "rod_02")["fragment_drop_chance"] = 1.0;
        });
        var (game, clock, _) = TestSupport.NewGame(config);
        var required = config.Rods.Rods.Single(r => r.Id == "rod_01").FragmentsRequired;
        game.Fishing.StartFishing();

        // Map 1: every fish pulled out gives 1 fragment of rod 1 and 1 of rod 2 (two rolls, in tier order).
        var updates = Cycles(game, clock, required - 1);
        Assert.All(updates, u => Assert.Equal(new[] { "rod_01", "rod_02" }, u.RodFragments.Select(f => f.RodId)));
        Assert.Equal("rod_00_starter", game.Profile.GetProfile().EquippedRod.RodId);
        var offer = game.Shop.GetShop().Rods.Single(r => r.RodId == "rod_01");
        Assert.Equal(required - 1, offer.Fragments);
        Assert.Equal(1.0, offer.FragmentChance, 9);
        Assert.False(offer.Owned);

        var last = Cycles(game, clock, 1).Single().RodFragments.Single(f => f.RodId == "rod_01");
        Assert.True(last.Assembled);
        Assert.True(last.Equipped);
        Assert.Equal(0, last.Have);
        var rod = game.Profile.GetProfile().EquippedRod;
        Assert.Equal("rod_01", rod.RodId);
        Assert.Equal(1, rod.Level);
        Assert.Equal(2, game.Profile.GetProfile().Inventory.Count);

        // Owned: new fragments are kept as extras.
        var extra = Cycles(game, clock, 1).Single().RodFragments.Single(f => f.RodId == "rod_01");
        Assert.False(extra.Assembled);
        Assert.True(extra.Owned);
        Assert.Equal(1, RodFragmentRules.Have(game.Session.Save, "rod_01"));
        Assert.Equal(required + 1, RodFragmentRules.Have(game.Session.Save, "rod_02"));

        // Map 2 (which had no rod before M24-T47) and map 10 drop both rods too.
        game.Session.Save.CurrentMapId = "map_02";
        Assert.Equal(10, Cycles(game, clock, 5).SelectMany(u => u.RodFragments).Count());
        TestSupport.GiveRod(game, "rod_05");
        game.Session.Save.FisherLevel = 90;
        game.Session.Save.CurrentMapId = "map_10";
        Assert.Equal(6, Cycles(game, clock, 3).SelectMany(u => u.RodFragments).Count());
        Assert.Equal(1 + 5 + 3, RodFragmentRules.Have(game.Session.Save, "rod_01"));
        Assert.Equal(required + 1 + 5 + 3, RodFragmentRules.Have(game.Session.Save, "rod_02"));
    }

    [Fact]
    public void Every_kind_of_fragment_rolls_on_its_own_after_the_catch()
    {
        // All 11 kinds at 100%: one fish pulled out gives 1 of each boat, 1 of each rod and 1 Fragmento de Roupa.
        var config = With((rods, equipment) =>
        {
            foreach (var r in rods["rods"]!.Where(r => r["fragment_drop_chance"] != null))
            {
                r["fragment_drop_chance"] = 1.0;
                r["fragments_required"] = 1_000_000;
            }

            foreach (var b in equipment["boats"]!.Where(b => b["fragment_drop_chance"] != null))
            {
                b["fragment_drop_chance"] = 1.0;
                b["fragments_required"] = 1_000_000;
            }

            equipment["clothing"]!["fragment_drop_chance"] = 1.0;
        });
        var (game, clock, _) = TestSupport.NewGame(config);
        game.Session.Save.CurrentMapId = "map_03";
        game.Session.Save.FisherLevel = 20;
        TestSupport.GiveRod(game, "rod_01");
        game.Fishing.StartFishing();
        var update = Cycles(game, clock, 1).Single();
        Assert.Single(update.NewCatches);
        Assert.Equal(new[] { "boat_01", "boat_02", "boat_03", "boat_04", "boat_05" }, update.Fragments.Select(f => f.BoatId));
        Assert.Equal(new[] { "rod_01", "rod_02", "rod_03", "rod_04", "rod_05" }, update.RodFragments.Select(f => f.RodId));
        Assert.Equal(1, update.ClothingFragments);
    }

    [Fact]
    public void A_rod_of_a_lower_tier_is_assembled_but_not_put_in_use()
    {
        var (game, _, _) = TestSupport.NewGame(Real);
        TestSupport.GiveRod(game, "rod_03");
        var gain = TestSupport.GiveRod(game, "rod_01");
        Assert.False(gain.Equipped);
        Assert.Equal("rod_03", game.Profile.GetProfile().EquippedRod.RodId);
        Assert.Contains(game.Profile.GetProfile().Inventory, r => r.RodId == "rod_01");
    }

    [Fact]
    public void Offline_fishing_drops_rod_and_clothing_fragments()
    {
        var config = With((rods, equipment) =>
        {
            NoDrops(rods, equipment);
            RodJson(rods, "rod_01")["fragment_drop_chance"] = 1.0;
            RodJson(rods, "rod_01")["fragments_required"] = 1_000_000;
            equipment["clothing"]!["fragment_drop_chance"] = 1.0;
        });
        var dir = TestSupport.NewTempDirectory();
        var clock = new ManualClock(TestSupport.StartMs);
        var (game, _, _) = TestSupport.NewGame(config, dir, clock);
        game.Fishing.StartFishing();
        game.Fishing.MarkSeen();

        clock.AdvanceSeconds(10 * config.Fishing.OfflineCycleSeconds);
        var (back, _, _) = TestSupport.NewGame(config, dir, clock);
        var update = back.Fishing.TakeOfflineReport().Update;
        Assert.Equal(10, update.NewCatches.Count);
        Assert.Equal(10, update.RodFragments.Count);
        Assert.Equal(10, update.ClothingFragments);
        Assert.Equal(10, RodFragmentRules.Have(back.Session.Save, "rod_01"));
        Assert.Equal(10, back.Clothing.GetClothing().Fragments);
    }

    // ------------------------------------------------------------------ rods out of the Shop, and the rods players have

    [Fact]
    public void Rods_are_not_sold_anymore_and_the_shop_shows_the_chance()
    {
        Assert.Null(typeof(RodOfferView).GetProperty("PriceCoins"));
        Assert.Null(typeof(RodAcquisitionConfig).GetProperty("PurchaseCostCoins"));
        var (game, _, _) = TestSupport.NewGame(Real);
        game.Session.Save.Coins = long.MaxValue / 2;
        game.Session.Save.Shells = long.MaxValue / 2;

        foreach (var offer in game.Shop.GetShop().Rods)
        {
            Assert.Equal(ServiceError.RodNotForSale, game.Shop.BuyRod(offer.RodId).Error);
            Assert.Equal(ServiceError.RodNotForSale, offer.BuyBlocker);
            Assert.True(offer.FragmentsRequired > 0);
            Assert.Equal(0.001, offer.FragmentChance, 9);
        }

        Assert.Equal(5, game.Shop.GetShop().Rods.Count);
        Assert.Equal(long.MaxValue / 2, game.Session.Save.Coins);
    }

    [Fact]
    public void A_version_17_save_keeps_its_rods_and_levels_and_starts_with_no_fragments()
    {
        var (game, clock, dir) = TestSupport.NewGame(Real);
        TestSupport.GiveRod(game, "rod_02");
        var item = game.Session.Save.EquippedRodItem();
        item.Level = 37;
        item.PurchasePriceCoins = 90_000;   // bought in the Shop before M24-T32
        game.Session.Persist();

        var path = Path.Combine(dir, JsonFilePlayerRepository.SaveFileName);
        var json = JObject.Parse(File.ReadAllText(path));
        json["save_version"] = 17;
        json.Remove("rod_fragments");
        json.Remove("clothing");
        File.WriteAllText(path, json.ToString());

        var reopened = TestSupport.NewGame(Real, dir, clock).Game;
        var save = reopened.Session.Save;
        Assert.Equal(PlayerSave.CurrentVersion, save.SaveVersion);
        Assert.True(PlayerSave.CurrentVersion >= 18);
        var rod = reopened.Profile.GetProfile().EquippedRod;
        Assert.Equal("rod_02", rod.RodId);
        Assert.Equal(37, rod.Level);
        Assert.Empty(save.RodFragments);
        Assert.Equal(0, save.Clothing.Fragments);
        Assert.Empty(save.Clothing.Pieces);
        Assert.True(reopened.Shop.GetShop().Rods.Single(r => r.RodId == "rod_02").Owned);
        Assert.Contains("\"save_version\": " + PlayerSave.CurrentVersion, File.ReadAllText(path));

        Assert.Contains("rod fragments malformed", SaveValidator.Validate(new PlayerSave
        {
            PlayerId = "p", CurrentMapId = "map_01", RodFragments = new Dictionary<string, long> { ["rod_01"] = -1 },
        }));
        Assert.Contains("clothing malformed", SaveValidator.Validate(new PlayerSave
        {
            PlayerId = "p", CurrentMapId = "map_01", Clothing = new ClothingState { Fragments = -1 },
        }));
    }

    [Fact]
    public void A_sold_rod_can_be_assembled_again_from_its_extras()
    {
        var (game, _, _) = TestSupport.NewGame(Real);
        var config = game.Session.Config;
        config.TryGetRod("rod_01", out var rod);
        TestSupport.GiveRod(game, "rod_01");
        RodFragmentRules.Add(config, game.Session.Save, rod, rod.FragmentsRequired - 1, 0);
        var starter = game.Profile.GetProfile().Inventory.Single(r => r.RodId == "rod_00_starter");
        game.Profile.EquipRod(starter.ItemId);
        var copy = game.Profile.GetProfile().Inventory.Single(r => r.RodId == "rod_01");
        Assert.True(game.Profile.SellRod(copy.ItemId).Succeeded);

        var gain = RodFragmentRules.Add(config, game.Session.Save, rod, 1, 0);
        Assert.True(gain.Assembled);
        Assert.Contains(game.Profile.GetProfile().Inventory, r => r.RodId == "rod_01");
    }

    // ------------------------------------------------------------------ clothing

    [Fact]
    public void There_are_five_pieces_one_axis_each()
    {
        var config = Real;
        var pieces = config.ClothingPieces.ToDictionary(p => p.Id, p => p.BonusAxis);
        Assert.Equal(new Dictionary<string, string>
        {
            ["gloves"] = ClothingAxes.CatchSuccess, ["hat"] = ClothingAxes.RarityEfficiency, ["boots"] = ClothingAxes.SizeQuality,
            ["shirt"] = ClothingAxes.ShellYield, ["pants"] = ClothingAxes.FisherXp,
        }, pieces);
        Assert.Equal(new[] { "Luvas", "Chapéu", "Botas", "Camiseta", "Calça" }, config.ClothingPieces.Select(p => p.DisplayName));
        Assert.Equal(100, config.ClothingMaxLevel);
        Assert.InRange(config.Clothing.FragmentDropChance, 0.0001, 0.05);
        Assert.Equal(5, config.Clothing.FragmentsToAssemble.Count);

        // Nv.1 small; Nv.100 never more than a middle rod (Corrente Mestra) at Nv.100 on the same axis.
        config.TryGetRod("rod_03", out var middle);
        var rodMax = config.RodBonusesAt(middle, 100);
        foreach (var piece in config.ClothingPieces)
        {
            Assert.InRange(config.ClothingBonusAt(piece, 1), 0.001, 0.05);
            var max = config.ClothingBonusAt(piece, 100);
            switch (piece.BonusAxis)
            {
                case ClothingAxes.CatchSuccess: Assert.True(max <= rodMax.CatchSuccess); break;
                case ClothingAxes.RarityEfficiency: Assert.True(max <= rodMax.RarityEfficiency); break;
                case ClothingAxes.SizeQuality: Assert.True(max <= rodMax.SizeQuality); break;
                case ClothingAxes.ShellYield: Assert.True(max <= rodMax.ShellYield); break;
                case ClothingAxes.FisherXp: Assert.InRange(max, 0.01, 0.3); break;
            }

            for (var level = 2; level <= 100; level++)
            {
                Assert.True(config.ClothingBonusAt(piece, level) > config.ClothingBonusAt(piece, level - 1));
            }
        }
    }

    [Fact]
    public void One_kind_of_clothing_fragment_drops_on_every_map()
    {
        var config = With((rods, equipment) =>
        {
            NoDrops(rods, equipment);
            equipment["clothing"]!["fragment_drop_chance"] = 1.0;
        });
        var (game, clock, _) = TestSupport.NewGame(config);
        TestSupport.GiveRod(game, "rod_05");
        game.Fishing.StartFishing();
        foreach (var map in config.Maps.Maps)
        {
            game.Session.Save.CurrentMapId = map.Id;
            var updates = Cycles(game, clock, 2);
            Assert.Equal(2, updates.Sum(u => u.ClothingFragments));
        }

        Assert.Equal(2 * config.Maps.Maps.Count, game.Clothing.GetClothing().Fragments);
    }

    [Fact]
    public void Any_piece_can_be_assembled_and_the_next_one_costs_more()
    {
        var (game, _, _) = TestSupport.NewGame(Real);
        var config = game.Session.Config;
        var costs = config.Clothing.FragmentsToAssemble;
        var view = game.Clothing.GetClothing();
        Assert.Equal(costs[0], view.NextCost);
        Assert.All(view.Pieces, p => Assert.Equal(ServiceError.NotEnoughClothingFragments, p.AssembleBlocker));
        Assert.Equal(ServiceError.NotEnoughClothingFragments, game.Clothing.Assemble("boots").Error);
        Assert.Equal(ServiceError.ClothingNotFound, game.Clothing.Assemble("scarf").Error);

        ClothingRules.AddFragments(game.Session.Save, costs[0] + costs[1] - 1);
        var boots = game.Clothing.Assemble("boots");
        Assert.True(boots.Succeeded, boots.ErrorMessage);
        var piece = boots.Value.Pieces.Single(p => p.PieceId == "boots");
        Assert.True(piece.Owned);
        Assert.Equal(1, piece.Level);
        Assert.Equal(costs[1] - 1, boots.Value.Fragments);
        Assert.Equal(costs[1], boots.Value.NextCost);
        Assert.Equal(ServiceError.ClothingAlreadyOwned, game.Clothing.Assemble("boots").Error);
        Assert.Equal(ServiceError.NotEnoughClothingFragments, game.Clothing.Assemble("hat").Error);

        ClothingRules.AddFragments(game.Session.Save, 1 + costs.Skip(2).Sum() + 7);
        foreach (var id in new[] { "hat", "shirt", "gloves", "pants" })
        {
            Assert.True(game.Clothing.Assemble(id).Succeeded);
        }

        var all = game.Clothing.GetClothing();
        Assert.Equal(5, all.Assembled);
        Assert.Equal(0, all.NextCost);
        Assert.Equal(7, all.Fragments);   // the extras stay (OD-060)

        // Kept between sessions.
        var save = game.Session.Save;
        Assert.Equal(5, save.Clothing.Pieces.Count);
    }

    [Fact]
    public void Each_piece_adds_only_to_its_own_axis()
    {
        foreach (var piece in Real.ClothingPieces)
        {
            var (game, _, _) = TestSupport.NewGame(Real);
            var config = game.Session.Config;
            var save = game.Session.Save;
            var gearBefore = game.Gear.GetGear().TotalBonus;
            ClothingRules.AddFragments(save, config.ClothingAssembleCost(0));
            Assert.True(game.Clothing.Assemble(piece.Id).Succeeded);
            save.Clothing.Pieces[piece.Id].Level = 100;
            var b = ClothingRules.Bonuses(config, save);
            var max = config.ClothingBonusAt(piece, 100);
            Assert.Equal(piece.BonusAxis == ClothingAxes.CatchSuccess ? max : 0, b.CatchSuccess, 9);
            Assert.Equal(piece.BonusAxis == ClothingAxes.RarityEfficiency ? max : 0, b.RarityEfficiency, 9);
            Assert.Equal(piece.BonusAxis == ClothingAxes.SizeQuality ? max : 0, b.SizeQuality, 9);
            Assert.Equal(piece.BonusAxis == ClothingAxes.ShellYield ? max : 0, b.ShellYield, 9);
            Assert.Equal(piece.BonusAxis == ClothingAxes.FisherXp ? max : 0, b.FisherXp, 9);

            // The Luvas show in the chance of the gear; the others do not.
            var gear = game.Gear.GetGear();
            Assert.Equal(gearBefore + (piece.BonusAxis == ClothingAxes.CatchSuccess ? max : 0), gear.TotalBonus, 9);

            // Rarity, size and Conchas add to the rod's bonuses in the catch.
            config.TryGetRod("rod_00_starter", out var rod);
            var withClothing = CatchRules.WithClothing(config.RodBonusesAt(rod, 1), b);
            Assert.Equal(piece.BonusAxis == ClothingAxes.RarityEfficiency ? max : 0, withClothing.RarityEfficiency, 9);
            Assert.Equal(piece.BonusAxis == ClothingAxes.SizeQuality ? max : 0, withClothing.SizeQuality, 9);
            Assert.Equal(piece.BonusAxis == ClothingAxes.ShellYield ? max : 0, withClothing.ShellYield, 9);
        }
    }

    [Fact]
    public void A_piece_levels_up_with_use_one_point_per_fish_at_no_cost()
    {
        var config = With(NoDrops);
        var (game, clock, _) = TestSupport.NewGame(config);
        var save = game.Session.Save;
        ClothingRules.AddFragments(save, config.ClothingAssembleCost(0));
        Assert.True(game.Clothing.Assemble("gloves").Succeeded);
        var coins = save.Coins;
        var shells = save.Shells;
        game.Fishing.StartFishing();

        var need = config.ClothingXpToNextLevel(1);
        var updates = Cycles(game, clock, (int)need);
        var level = updates.SelectMany(u => u.ClothingLevels).Single();
        Assert.Equal("gloves", level.PieceId);
        Assert.Equal(2, level.Level);
        Assert.Equal(2, save.Clothing.Pieces["gloves"].Level);
        Assert.Equal(0, save.Clothing.Pieces["gloves"].Xp);

        Cycles(game, clock, 3);
        var view = game.Clothing.GetClothing().Pieces.Single(p => p.PieceId == "gloves");
        Assert.Equal(3, view.Xp);
        Assert.Equal(config.ClothingXpToNextLevel(2), view.XpToNext);
        Assert.Equal(config.ClothingBonusAt(config.ClothingPieces.Single(p => p.Id == "gloves"), 2), view.Bonus, 9);
        Assert.Equal(coins, save.Coins);   // levels by use cost nothing
        Assert.True(save.Shells >= shells);

        // At the max level it stops counting.
        save.Clothing.Pieces["gloves"].Level = config.ClothingMaxLevel;
        Assert.Empty(Cycles(game, clock, 3).SelectMany(u => u.ClothingLevels));
        Assert.Equal(0, save.Clothing.Pieces["gloves"].Xp);
    }

    [Fact]
    public void The_calca_adds_fisher_xp_and_no_clothing_changes_the_catches()
    {
        // The same seed with and without the pants at Nv.100: the same fish, and the pants add their % of each fish's XP.
        var config = With(NoDrops);
        LocalGame Play(bool pants, out List<CatchView> catches, out long xp)
        {
            var (game, clock, _) = TestSupport.NewGame(config);
            game.Session.Save.RngSeed = 4242;
            if (pants)
            {
                ClothingRules.AddFragments(game.Session.Save, config.ClothingAssembleCost(0));
                Assert.True(game.Clothing.Assemble("pants").Succeeded);
                game.Session.Save.Clothing.Pieces["pants"].Level = 100;
            }

            game.Fishing.StartFishing();
            var updates = Cycles(game, clock, 40);
            catches = updates.SelectMany(u => u.NewCatches).ToList();
            xp = updates.Sum(u => u.XpGained);
            return game;
        }

        Play(false, out var plain, out var plainXp);
        var dressed = Play(true, out var withPants, out var pantsXp);
        Assert.Equal(plain.Select(c => c.SpeciesId + ":" + c.SizeCm), withPants.Select(c => c.SpeciesId + ":" + c.SizeCm));
        var bonus = config.ClothingBonusAt(config.ClothingPieces.Single(p => p.Id == "pants"), 100);
        Assert.True(pantsXp > plainXp);
        Assert.InRange(pantsXp - plainXp, (long)(plainXp * bonus) - 40, (long)(plainXp * bonus) + 40);
        Assert.NotNull(dressed);
    }

    [Fact]
    public void Rod_and_clothing_rolls_come_after_the_catch_so_the_catches_never_change()
    {
        // Rod and clothing fragments at 50% (the rod never assembles): the same seed gives the same fish as with no drops.
        // Since M24-T47 every boat and rod rolls on every catch too: with all of them at 50% the fish are still the same.
        var none = With(NoDrops);
        var drops = With((rods, equipment) =>
        {
            NoDrops(rods, equipment);
            RodJson(rods, "rod_01")["fragment_drop_chance"] = 0.5;
            RodJson(rods, "rod_01")["fragments_required"] = 1_000_000;
            equipment["clothing"]!["fragment_drop_chance"] = 0.5;
        });
        var everything = With((rods, equipment) =>
        {
            foreach (var r in rods["rods"]!.Where(r => r["fragment_drop_chance"] != null))
            {
                r["fragment_drop_chance"] = 0.5;
                r["fragments_required"] = 1_000_000;
            }

            foreach (var b in equipment["boats"]!.Where(b => b["fragment_drop_chance"] != null))
            {
                b["fragment_drop_chance"] = 0.5;
                b["fragments_required"] = 1_000_000;
            }

            equipment["clothing"]!["fragment_drop_chance"] = 0.5;
        });

        List<string> Run(GameConfig config, out int rodFragments, out long clothingFragments)
        {
            var (game, clock, _) = TestSupport.NewGame(config);
            game.Session.Save.RngSeed = 99;
            game.Fishing.StartFishing();
            var updates = Cycles(game, clock, 200);
            rodFragments = updates.Sum(u => u.RodFragments.Count);
            clothingFragments = updates.Sum(u => u.ClothingFragments);
            return updates.SelectMany(u => u.NewCatches).Select(c => c.SpeciesId + ":" + c.SizeCm + ":" + c.SizeCategoryId).ToList();
        }

        var plain = Run(none, out var r0, out var c0);
        var withDrops = Run(drops, out var r1, out var c1);
        Assert.Equal(plain, withDrops);
        Assert.Equal(plain, Run(everything, out var rAll, out _));
        Assert.InRange(rAll, 5 * 60, 5 * 140);
        Assert.Equal(0, r0 + c0);
        Assert.InRange(r1, 60, 140);
        Assert.InRange(c1, 60, 140);

        // And the same seed gives the same fragments.
        Run(drops, out var r2, out var c2);
        Assert.Equal(r1, r2);
        Assert.Equal(c1, c2);
    }

    [Fact]
    public void The_best_gear_with_the_luvas_still_stops_at_the_95_percent_ceiling()
    {
        var config = Real;
        config.TryGetRod("rod_05", out var rod);
        config.TryGetBoat("boat_05", out var boat);
        var bait = config.Equipment.Baits.OrderByDescending(b => b.CatchSuccessBonus).First();
        var gloves = config.ClothingBonusAt(config.ClothingPieces.Single(p => p.BonusAxis == ClothingAxes.CatchSuccess), 100);
        var bonus = config.RodBonusesAt(rod, 100).CatchSuccess + config.BoatBonusAt(boat, 100) + bait.CatchSuccessBonus + gloves;
        foreach (var tier in config.Progression.Rarity.Tiers)
        {
            Assert.Equal(0.95, CatchRules.SuccessChance(config, tier.Id, bonus), 9);
        }

        var (game, _, _) = TestSupport.NewGame(config);
        var save = game.Session.Save;
        ClothingRules.AddFragments(save, config.ClothingAssembleCost(0));
        game.Clothing.Assemble("gloves");
        save.Clothing.Pieces["gloves"].Level = 100;
        TestSupport.GiveRod(game, "rod_05");
        save.EquippedRodItem().Level = 100;
        Assert.All(game.Gear.GetGear().Chances, c => Assert.True(c.Chance <= 0.95 + 1e-9));
    }

    [Fact]
    public void Broken_clothing_configs_are_rejected_in_portuguese()
    {
        var axis = TestSupport.ConfigWith(GameConfigLoader.EquipmentFile, j => j.Replace("\"bonus_axis\": \"fisher_xp\"", "\"bonus_axis\": \"luck\""));
        Assert.False(axis.Succeeded);
        Assert.Contains(axis.Errors, e => e.Contains("pants") && e.Contains("bonus_axis"));

        var chance = TestSupport.ConfigWith(GameConfigLoader.EquipmentFile, j =>
        {
            var root = JObject.Parse(j);
            root["clothing"]!["fragment_drop_chance"] = 1.5;
            return root.ToString();
        });
        Assert.False(chance.Succeeded);
        Assert.Contains(chance.Errors, e => e.Contains("clothing.fragment_drop_chance"));

        var costs = TestSupport.ConfigWith(GameConfigLoader.EquipmentFile, j =>
        {
            var root = JObject.Parse(j);
            root["clothing"]!["fragments_to_assemble"] = new JArray();
            return root.ToString();
        });
        Assert.False(costs.Succeeded);
        Assert.Contains(costs.Errors, e => e.Contains("fragments_to_assemble"));
    }

    // ------------------------------------------------------------------ the pass

    [Fact]
    public void The_pass_gives_rod_fragments_where_a_map_asks_for_a_new_rod_and_clothing_fragments_everywhere()
    {
        var config = Real;
        var expected = new Dictionary<int, string> { [10] = "rod_01", [30] = "rod_02", [50] = "rod_03", [70] = "rod_04", [90] = "rod_05" };
        foreach (var m in config.Pass.Milestones)
        {
            Assert.True(m.Free.ClothingFragments > 0, "Nv." + m.Level);
            Assert.True(m.Premium.ClothingFragments > m.Free.ClothingFragments, "Nv." + m.Level);
            if (expected.TryGetValue(m.Level, out var rodId))
            {
                config.TryGetRod(rodId, out var rod);
                Assert.Equal(rodId, m.Free.RodId);
                Assert.Equal((int)Math.Ceiling(rod.FragmentsRequired * 0.15), m.Free.RodFragments);
                Assert.Equal(m.Free.RodFragments * 2, m.Premium.RodFragments);
            }
            else
            {
                Assert.Equal(0, m.Free.RodFragments);
                Assert.Null(m.Free.RodId);
            }
        }

        var unknown = TestSupport.ConfigWith(GameConfigLoader.PassFile, j =>
        {
            var root = JObject.Parse(j);
            root["milestones"]![0]!["free"]!["rod_id"] = "rod_99";
            return root.ToString();
        });
        Assert.False(unknown.Succeeded);
        Assert.Contains(unknown.Errors, e => e.Contains("pass.json") && e.Contains("rod_99"));
    }

    [Fact]
    public void Claiming_a_milestone_gives_its_rod_and_clothing_fragments()
    {
        var (game, _, _) = TestSupport.NewGame(Real);
        var save = game.Session.Save;
        var config = game.Session.Config;
        var reward = config.PassMilestoneAt(10).Free;
        config.TryGetRod(reward.RodId, out var rod);
        RodFragmentRules.Add(config, save, rod, rod.FragmentsRequired - reward.RodFragments, 0);
        save.FisherLevel = 10;

        var view = game.Pass.GetPass().Milestones[0].Free;
        Assert.Equal(reward.RodFragments, view.RodFragments);
        Assert.Equal(rod.DisplayName, view.RodName);
        Assert.Equal(reward.ClothingFragments, view.ClothingFragments);

        var claim = game.Pass.Claim(10, PassTrack.Free);
        Assert.True(claim.Succeeded, claim.ErrorMessage);
        Assert.Equal(reward.RodFragments, claim.Value.RodFragments.Single().Amount);
        Assert.Contains(rod.DisplayName, claim.Value.RodsAssembled);
        Assert.Equal(reward.ClothingFragments, claim.Value.ClothingFragments);
        Assert.Equal(reward.ClothingFragments, save.Clothing.Fragments);
        Assert.Equal(rod.Id, game.Profile.GetProfile().EquippedRod.RodId);
    }

    // ------------------------------------------------------------------ the shop's views

    [Fact]
    public void The_clothing_view_shows_levels_xp_and_bonuses()
    {
        var (game, _, _) = TestSupport.NewGame(Real);
        var config = game.Session.Config;
        var view = game.Clothing.GetClothing();
        Assert.Equal(5, view.Pieces.Count);
        Assert.Equal(config.Clothing.FragmentDropChance, view.FragmentChance);
        Assert.All(view.Pieces, p =>
        {
            Assert.False(p.Owned);
            Assert.Equal(0, p.Level);
            Assert.Equal(100, p.MaxLevel);
            Assert.True(p.BonusAtMax > p.BonusAtLevel1);
            Assert.Equal(p.BonusAtLevel1, p.Bonus, 9);
        });
        Assert.True(view.Bonuses.IsNone);
    }
}
