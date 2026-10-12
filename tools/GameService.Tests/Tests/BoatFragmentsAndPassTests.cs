using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Pass;
using FishingIdle.GameService.Persistence;
using FishingIdle.GameService.Shop;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>
/// Boats by fragments (M24-T16, A-157) and the Passe do Pescador (M24-T17, A-158): fragments per catch with the service's
/// Rng, assembling a boat, boats out of the Shop, saves that keep their boats, the pass rewards, the premium bought with
/// Dólares (also for the milestones already passed) and the Dólares of A-111 never paid twice.
/// </summary>
public sealed class BoatFragmentsAndPassTests
{
    private static GameConfig Config => TestSupport.RealConfig();

    /// <summary>The certain-catch config with the fragment chance of one boat (and, if asked, offline drops) changed.</summary>
    private static GameConfig WithFragmentChance(string boatId, double chance, bool dropOffline = true, double extraMapChance = -1)
    {
        var texts = TestSupport.ConfigTexts();
        var progression = JObject.Parse(texts[GameConfigLoader.ProgressionFile]);
        foreach (var tier in progression["rarity"]!["tiers"]!)
        {
            tier["catch_success_base"] = 1.0;
        }

        progression["fishing"]!["catch_success_max"] = 1.0;
        texts[GameConfigLoader.ProgressionFile] = progression.ToString();

        var equipment = JObject.Parse(texts[GameConfigLoader.EquipmentFile]);
        var boatJson = equipment["boats"]!.Single(b => (string)b["id"]! == boatId);
        boatJson["fragment_drop_chance"] = chance;
        if (extraMapChance >= 0 && boatJson["extra_fragment_maps"] is JArray extras)
        {
            foreach (var extra in extras)
            {
                extra["fragment_drop_chance"] = extraMapChance;
            }
        }

        equipment["boat_fragments"]!["drop_on_offline_fishing"] = dropOffline;
        texts[GameConfigLoader.EquipmentFile] = equipment.ToString();

        var result = GameConfigLoader.LoadFromTexts(texts);
        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        return result.Config;
    }

    // ------------------------------------------------------------------ the 5 boats (M24-T42, A-168)

    [Fact]
    public void There_are_five_boats_and_maps_6_to_10_drop_boats_1_to_5_again()
    {
        var config = Config;
        var boats = config.Equipment.Boats.OrderBy(b => b.Tier).ToList();
        Assert.Equal(6, boats.Count);
        Assert.Null(config.StarterBoat.MapId);
        Assert.False(config.StarterBoat.HasLevels);
        Assert.False(config.TryGetBoat("boat_06", out _));
        Assert.False(config.TryGetBoat("boat_10", out _));

        var maps = config.Maps.Maps.OrderBy(m => m.UnlockFisherLevel).ToList();
        Assert.Equal(10, maps.Count);
        for (var i = 0; i < 10; i++)
        {
            // Map 1 → boat 1 … map 5 → boat 5; map 6 → boat 1 … map 10 → boat 5.
            var boat = boats[i % 5 + 1];
            Assert.True(config.TryGetBoatForMap(maps[i].Id, out var forMap));
            Assert.Equal(boat.Id, forMap.Id);
            Assert.InRange(config.BoatFragmentChanceOn(maps[i].Id), 0.003, 0.03);
            if (i < 5)
            {
                Assert.Equal(maps[i].Id, boat.MapId);
                Assert.Equal(boat.FragmentDropChance, config.BoatFragmentChanceOn(maps[i].Id), 9);
                Assert.True(boat.HasLevels);
                Assert.Equal(boat.CatchSuccessBonus * 3, boat.CatchSuccessBonusAtMaxLevel, 6);
                if (i > 0)
                {
                    Assert.True(boat.FragmentsRequired > boats[i].FragmentsRequired, boat.Id + " asks more fragments than the one before");
                    Assert.True(boat.CatchSuccessBonus > boats[i].CatchSuccessBonus, boat.Id + " is better than the one before");
                }
            }
            else
            {
                Assert.Contains(boat.ExtraFragmentMaps, e => e.MapId == maps[i].Id);
            }
        }

        // Maps 1 to 5 keep their chances; maps 6 to 10 keep the chance they had for boats 6 to 10 (A-157 → A-168).
        var expected = new Dictionary<string, double>
        {
            ["map_01"] = 0.0043, ["map_02"] = 0.0043, ["map_03"] = 0.0048, ["map_04"] = 0.0051, ["map_05"] = 0.0052,
            ["map_06"] = 0.0053, ["map_07"] = 0.0058, ["map_08"] = 0.0061, ["map_09"] = 0.0066, ["map_10"] = 0.0067,
        };
        foreach (var pair in expected)
        {
            Assert.Equal(pair.Value, config.BoatFragmentChanceOn(pair.Key), 9);
        }

        Assert.Equal(20, boats[1].FragmentsRequired);
        Assert.Equal(55, boats[5].FragmentsRequired);
    }

    [Fact]
    public void A_catch_on_map_6_drops_a_fragment_of_boat_1_and_the_shop_names_both_maps()
    {
        var (game, clock, _) = TestSupport.NewGame(WithFragmentChance("boat_01", 1.0, extraMapChance: 1.0));
        var save = game.Session.Save;
        save.CurrentMapId = "map_06";
        save.FisherLevel = game.Session.Config.Maps.Maps.Single(m => m.Id == "map_06").UnlockFisherLevel;
        TestSupport.GiveRod(game, "rod_05");
        game.Fishing.StartFishing();
        clock.AdvanceSeconds(game.Session.Config.Fishing.OnlineCycleSeconds);
        var fragments = game.Fishing.Sync().Fragments;
        Assert.Single(fragments);
        Assert.Equal("boat_01", fragments[0].BoatId);

        var offer = game.Gear.GetGear().Boats.Single(b => b.BoatId == "boat_01");
        Assert.Equal(new[] { "Lago Sereno", game.Session.Config.Maps.Maps.Single(m => m.Id == "map_06").DisplayName }, offer.MapNames);
        Assert.Equal(1.0, offer.FragmentChance, 9);
    }

    [Fact]
    public void The_best_boat_at_level_100_still_stops_at_the_catch_success_ceiling()
    {
        var config = Config;
        config.TryGetRod("rod_05", out var rod);
        config.TryGetBoat("boat_05", out var boat);
        var bonus = config.RodBonusesAt(rod, 100).CatchSuccess + config.BoatBonusAt(boat, 100);
        foreach (var tier in config.Progression.Rarity.Tiers)
        {
            Assert.Equal(config.Fishing.CatchSuccessMax, Fishing.CatchRules.SuccessChance(config, tier.Id, bonus), 9);
        }
    }

    [Fact]
    public void Boats_are_not_sold_anymore()
    {
        Assert.Null(typeof(IGearService).GetMethod("BuyBoat"));
        var (game, _, _) = TestSupport.NewGame(TestSupport.RealConfig());
        game.Session.Save.Coins = long.MaxValue / 2;
        game.Session.Save.Shells = long.MaxValue / 2;

        var view = game.Gear.GetGear();
        var boat1 = view.Boats.Single(b => b.BoatId == "boat_01");
        Assert.False(boat1.Owned);
        Assert.Equal("map_01", boat1.MapId);
        Assert.Equal("Lago Sereno", boat1.MapName);
        Assert.Equal(0, boat1.Fragments);
        Assert.Equal(20, boat1.FragmentsRequired);
        Assert.Equal(ServiceError.BoatNotOwned, game.Gear.UseBoat("boat_01").Error);
        Assert.Equal(ServiceError.BoatNotOwned, game.Gear.UpgradeBoat("boat_01").Error);
    }

    // ------------------------------------------------------------------ fragments per catch

    [Fact]
    public void Every_catch_on_the_map_can_drop_a_fragment_and_the_boat_assembles_itself()
    {
        // With the chance at 100%, every fish pulled out on map_01 gives 1 fragment of boat_01; 20 assemble it.
        var (game, clock, _) = TestSupport.NewGame(WithFragmentChance("boat_01", 1.0));
        var config = game.Session.Config;
        game.Fishing.StartFishing();
        var cycle = config.Fishing.OnlineCycleSeconds;

        var fragments = new List<FragmentGainView>();
        for (var i = 0; i < 19; i++)
        {
            clock.AdvanceSeconds(cycle);
            fragments.AddRange(game.Fishing.Sync().Fragments);
        }

        Assert.Equal(19, fragments.Count);
        Assert.All(fragments, f => Assert.Equal("boat_01", f.BoatId));
        Assert.Equal(19, game.Gear.GetGear().Boats.Single(b => b.BoatId == "boat_01").Fragments);
        Assert.Equal("boat_00", game.Gear.GetGear().BoatId);

        clock.AdvanceSeconds(cycle);
        var last = game.Fishing.Sync().Fragments.Single();
        Assert.True(last.Assembled);
        Assert.True(last.Owned);
        Assert.Equal(0, last.Have);

        var gear = game.Gear.GetGear();
        Assert.Equal("boat_01", gear.BoatId);
        var boat = gear.Boats.Single(b => b.BoatId == "boat_01");
        Assert.True(boat.Owned);
        Assert.Equal(1, boat.Level);

        // After the boat is owned, new fragments are kept as extras.
        clock.AdvanceSeconds(cycle);
        var extra = game.Fishing.Sync().Fragments.Single();
        Assert.False(extra.Assembled);
        Assert.True(extra.Owned);
        Assert.Equal(1, game.Gear.GetGear().Boats.Single(b => b.BoatId == "boat_01").Fragments);
    }

    [Fact]
    public void The_fragment_roll_uses_the_service_rng_and_repeats_with_the_same_seed()
    {
        int Run(ulong seed)
        {
            var (game, clock, _) = TestSupport.NewGame(WithFragmentChance("boat_01", 0.3));
            game.Session.Save.RngSeed = seed;
            game.Fishing.StartFishing();
            var total = 0;
            for (var i = 0; i < 60; i++)
            {
                clock.AdvanceSeconds(game.Session.Config.Fishing.OnlineCycleSeconds);
                total += game.Fishing.Sync().Fragments.Count;
            }

            return total;
        }

        var first = Run(777);
        Assert.Equal(first, Run(777));
        Assert.InRange(first, 5, 35);
    }

    [Fact]
    public void Only_the_boat_of_the_map_drops_and_a_zero_chance_drops_nothing()
    {
        var (game, clock, _) = TestSupport.NewGame(WithFragmentChance("boat_01", 0.0));
        game.Fishing.StartFishing();
        var catches = TestSupport.PlayFor(game, clock, 600, game.Session.Config.Fishing.OnlineCycleSeconds);
        Assert.NotEmpty(catches);
        Assert.Empty(game.Session.Save.BoatFragments);
    }

    [Fact]
    public void Offline_fishing_drops_fragments_when_the_config_allows_it()
    {
        foreach (var offline in new[] { true, false })
        {
            var saveDir = TestSupport.NewTempDirectory();
            var clock = new ManualClock(TestSupport.StartMs);
            var config = WithFragmentChance("boat_01", 1.0, offline);
            var (game, _, _) = TestSupport.NewGame(config, saveDir, clock);
            game.Fishing.StartFishing();
            game.Fishing.MarkSeen();

            // 10 offline cycles: fewer than the 20 fragments that assemble boat_01, so they all stay as fragments.
            clock.AdvanceSeconds(10 * config.Fishing.OfflineCycleSeconds);
            var (reopened, _, _) = TestSupport.NewGame(config, saveDir, clock);
            var report = reopened.Fishing.TakeOfflineReport();
            Assert.NotNull(report);
            Assert.NotEmpty(report.Update.NewCatches);
            if (offline)
            {
                Assert.Equal(report.Update.NewCatches.Count, report.Update.Fragments.Count);
                Assert.Equal(report.Update.NewCatches.Count, BoatFragmentRules.Have(reopened.Session.Save, "boat_01"));
            }
            else
            {
                Assert.Empty(report.Update.Fragments);
            }
        }
    }

    [Fact]
    public void A_new_boat_is_put_in_use_only_when_it_is_better()
    {
        var (game, _, _) = TestSupport.NewGame(TestSupport.RealConfig());
        TestSupport.GiveBoat(game, "boat_05");
        Assert.Equal("boat_05", game.Gear.GetGear().BoatId);

        TestSupport.GiveBoat(game, "boat_01");
        var gear = game.Gear.GetGear();
        Assert.Equal("boat_05", gear.BoatId);
        Assert.True(gear.Boats.Single(b => b.BoatId == "boat_01").Owned);
    }

    [Fact]
    public void The_starter_boat_takes_no_fragments_and_fragments_are_kept_between_sessions()
    {
        var (game, clock, dir) = TestSupport.NewGame(TestSupport.RealConfig());
        var config = game.Session.Config;
        Assert.Null(BoatFragmentRules.Add(config, game.Session.Save, config.StarterBoat, 5));

        config.TryGetBoat("boat_03", out var boat);
        var gain = BoatFragmentRules.Add(config, game.Session.Save, boat, 7);
        Assert.Equal(7, gain.Have);
        Assert.False(gain.Assembled);
        game.Session.Persist();

        var reopened = TestSupport.NewGame(TestSupport.RealConfig(), dir, clock).Game;
        Assert.Equal(7, reopened.Gear.GetGear().Boats.Single(b => b.BoatId == "boat_03").Fragments);
        Assert.Contains("boat fragments malformed", SaveValidator.Validate(new PlayerSave
        {
            PlayerId = "p", CurrentMapId = "map_01", BoatFragments = new Dictionary<string, long> { ["boat_01"] = -1 },
        }));
    }

    // ------------------------------------------------------------------ the pass

    [Fact]
    public void The_pass_has_100_milestones_with_the_old_level_dollars_in_the_free_track()
    {
        var pass = Config.Pass;
        Assert.Equal(100, pass.Milestones.Count);
        Assert.Equal(Enumerable.Range(1, 100).Select(i => i * 10), pass.Milestones.Select(m => m.Level));
        Assert.Equal(100, pass.PremiumPriceDollars);

        // A-111: 10 Dólares at Nv.10, 20, … 100, now in the free track; after that the pass sets them.
        Assert.All(pass.Milestones.Where(m => m.Level <= 100), m => Assert.Equal(10, m.Free.Dollars));
        Assert.Equal(100, pass.Milestones.Where(m => m.Level <= 100).Sum(m => m.Free.Dollars));
        Assert.True(pass.Milestones.Where(m => m.Level > 100).Sum(m => m.Premium.Dollars) > pass.Milestones.Where(m => m.Level > 100).Sum(m => m.Free.Dollars));

        // Fragments of the boat the matching map drops up to Nv.100 (maps 6 to 10 drop boats 1 to 5, A-168), then they take
        // turns; only the 5 boats. Every reward has Moedas and Conchas, growing.
        for (var i = 1; i <= 100; i++)
        {
            Assert.Equal("boat_" + ((i - 1) % 5 + 1).ToString("00"), pass.Milestones[i - 1].Free.BoatId);
            Assert.Equal(pass.Milestones[i - 1].Free.BoatId, pass.Milestones[i - 1].Premium.BoatId);
        }

        Assert.All(pass.Milestones, m =>
        {
            Assert.True(m.Free.Coins > 0 && m.Free.Shells > 0 && m.Free.Fragments > 0);
            Assert.True(m.Premium.Coins > m.Free.Coins && m.Premium.Fragments > m.Free.Fragments);
        });
        Assert.True(pass.Milestones[^1].Free.Coins > pass.Milestones[0].Free.Coins);
    }

    [Fact]
    public void A_reached_milestone_is_claimed_once_and_gives_its_reward()
    {
        var (game, _, _) = TestSupport.NewGame(TestSupport.RealConfig());
        var save = game.Session.Save;
        var config = game.Session.Config;
        Assert.Equal(config.Pass.PassId, save.Pass.PassId);
        Assert.Equal(0, game.Pass.GetPass().ClaimableCount);
        Assert.Equal(ServiceError.PassMilestoneNotReached, game.Pass.Claim(10, PassTrack.Free).Error);
        Assert.Equal(ServiceError.PassMilestoneNotFound, game.Pass.Claim(15, PassTrack.Free).Error);
        Assert.Equal(ServiceError.PassNothingToClaim, game.Pass.ClaimAll().Error);

        save.FisherLevel = 10;
        var view = game.Pass.GetPass();
        Assert.Equal(1, view.ClaimableCount);
        Assert.Equal(10, view.PreviousMilestoneLevel);
        Assert.Equal(20, view.NextMilestoneLevel);
        Assert.True(view.Milestones[0].Free.Claimable);
        Assert.True(view.Milestones[0].Premium.Locked);
        Assert.False(view.Milestones[0].Premium.Claimable);
        Assert.Equal(ServiceError.PassPremiumLocked, game.Pass.Claim(10, PassTrack.Premium).Error);

        var reward = config.PassMilestoneAt(10).Free;
        var claim = game.Pass.Claim(10, PassTrack.Free);
        Assert.True(claim.Succeeded, claim.ErrorMessage);
        Assert.Equal(reward.Coins, save.Coins);
        Assert.Equal(reward.Shells, save.Shells);
        Assert.Equal(reward.Dollars, save.Dollars);
        Assert.Equal(reward.Fragments, BoatFragmentRules.Have(save, reward.BoatId));
        Assert.Equal(reward.Fragments, claim.Value.Fragments.Single().Amount);
        Assert.Equal(ServiceError.PassAlreadyClaimed, game.Pass.Claim(10, PassTrack.Free).Error);
        Assert.True(claim.Value.Pass.Milestones[0].Free.Claimed);
        Assert.Equal(0, claim.Value.Pass.ClaimableCount);
    }

    [Fact]
    public void The_premium_costs_dollars_and_opens_the_premium_of_the_milestones_already_passed()
    {
        var (game, clock, dir) = TestSupport.NewGame(TestSupport.RealConfig());
        var save = game.Session.Save;
        var config = game.Session.Config;
        save.FisherLevel = 35;
        Assert.True(game.Pass.ClaimAll().Succeeded);   // Nv.10, 20, 30 free
        Assert.Equal(30, save.Dollars);
        Assert.Equal(ServiceError.NotEnoughDollars, game.Pass.BuyPremium().Error);

        save.Dollars = 120;
        var bought = game.Pass.BuyPremium();
        Assert.True(bought.Succeeded, bought.ErrorMessage);
        Assert.Equal(20, save.Dollars);
        Assert.True(bought.Value.Premium);
        Assert.Equal(3, bought.Value.ClaimableCount);   // the premium of Nv.10, 20, 30
        Assert.Equal(ServiceError.PassPremiumOwned, game.Pass.BuyPremium().Error);

        var coins = save.Coins;
        var all = game.Pass.ClaimAll();
        Assert.True(all.Succeeded);
        Assert.Equal(3, all.Value.Claimed);
        Assert.Equal(new[] { 10, 20, 30 }.Sum(l => config.PassMilestoneAt(l).Premium.Coins), save.Coins - coins);

        // Saved: reopening keeps the premium and what was claimed.
        var reopened = TestSupport.NewGame(TestSupport.RealConfig(), dir, clock).Game;
        var view = reopened.Pass.GetPass();
        Assert.True(view.Premium);
        Assert.Equal(0, view.ClaimableCount);
        Assert.Equal(new List<int> { 10, 20, 30 }, reopened.Session.Save.Pass.PremiumClaimed);
    }

    [Fact]
    public void Pass_fragments_can_assemble_a_boat()
    {
        var (game, _, _) = TestSupport.NewGame(TestSupport.RealConfig());
        var save = game.Session.Save;
        var config = game.Session.Config;
        config.TryGetBoat("boat_01", out var boat);
        BoatFragmentRules.Add(config, save, boat, boat.FragmentsRequired - 1);
        save.FisherLevel = 10;

        var claim = game.Pass.Claim(10, PassTrack.Free);
        Assert.True(claim.Succeeded);
        Assert.Contains(boat.DisplayName, claim.Value.BoatsAssembled);
        Assert.True(game.Gear.GetGear().Boats.Single(b => b.BoatId == "boat_01").Owned);
    }

    // ------------------------------------------------------------------ save version 16

    [Fact]
    public void A_version_15_save_keeps_its_boats_and_never_gets_the_level_dollars_twice()
    {
        var (game, clock, dir) = TestSupport.NewGame(TestSupport.RealConfig());
        var save = game.Session.Save;
        TestSupport.GiveBoat(game, "boat_02");
        save.BoatLevels["boat_02"] = 7;
        save.FisherLevel = 60;
        save.Dollars = 60;   // Nv.10 to Nv.60 paid by the old rule (A-111)
        game.Session.Persist();

        // The same player as a version 15 file: no fragments, no pass.
        var path = Path.Combine(dir, JsonFilePlayerRepository.SaveFileName);
        var json = JObject.Parse(File.ReadAllText(path));
        json["save_version"] = 15;
        json.Remove("boat_fragments");
        json.Remove("pass");
        File.WriteAllText(path, json.ToString());

        var reopened = TestSupport.NewGame(TestSupport.RealConfig(), dir, clock).Game;
        var moved = reopened.Session.Save;
        Assert.Equal(PlayerSave.CurrentVersion, moved.SaveVersion);
        Assert.Contains("boat_02", moved.OwnedBoatIds);
        Assert.Equal(7, reopened.Gear.GetGear().Boats.Single(b => b.BoatId == "boat_02").Level);
        Assert.Empty(moved.BoatFragments);
        Assert.Equal(60, moved.Pass.LegacyDollarsUpToLevel);
        Assert.Equal(reopened.Session.Config.Pass.PassId, moved.Pass.PassId);

        // The milestones up to Nv.60 are claimable with everything but their Dólares.
        var view = reopened.Pass.GetPass();
        Assert.Equal(6, view.ClaimableCount);
        Assert.All(view.Milestones.Take(6), m => Assert.True(m.Free.DollarsAlreadyPaid));
        Assert.False(view.Milestones[6].Free.DollarsAlreadyPaid);
        var claim = reopened.Pass.ClaimAll();
        Assert.True(claim.Succeeded);
        Assert.Equal(0, claim.Value.Dollars);
        Assert.True(claim.Value.Coins > 0);
        Assert.Equal(60, moved.Dollars);

        // Nv.70 was never paid: the pass pays it.
        moved.FisherLevel = 70;
        Assert.Equal(10, reopened.Pass.Claim(70, PassTrack.Free).Value.Dollars);
        Assert.Equal(70, moved.Dollars);

        // Up to Nv.100 the total is the same as the old rule: 100 Dólares, never more.
        moved.FisherLevel = 100;
        reopened.Pass.ClaimAll();
        Assert.Equal(100, moved.Dollars);
        Assert.Contains("\"save_version\": " + PlayerSave.CurrentVersion, File.ReadAllText(path));
    }

    [Fact]
    public void A_new_player_owes_no_legacy_dollars()
    {
        var (game, _, _) = TestSupport.NewGame(TestSupport.RealConfig());
        var save = game.Session.Save;
        Assert.Equal(0, save.Pass.LegacyDollarsUpToLevel);
        save.FisherLevel = 100;
        Assert.True(game.Pass.ClaimAll().Succeeded);
        Assert.Equal(100, save.Dollars);
    }

    // ------------------------------------------------------------------ the config is checked

    [Fact]
    public void Broken_boat_and_pass_configs_are_rejected_in_portuguese()
    {
        var unknownMap = TestSupport.ConfigWith(GameConfigLoader.EquipmentFile, j => j.Replace("\"map_id\": \"map_03\"", "\"map_id\": \"map_99\""));
        Assert.False(unknownMap.Succeeded);
        Assert.Contains(unknownMap.Errors, e => e.Contains("boat_03") && e.Contains("map_id"));

        var twice = TestSupport.ConfigWith(GameConfigLoader.EquipmentFile, j => j.Replace("\"map_id\": \"map_03\"", "\"map_id\": \"map_02\""));
        Assert.False(twice.Succeeded);
        Assert.Contains(twice.Errors, e => e.Contains("map_02") && e.Contains("mais de um barco"));

        var pass = TestSupport.ConfigWith(GameConfigLoader.PassFile, j =>
        {
            var root = JObject.Parse(j);
            root["milestones"]![0]!["free"]!["boat_id"] = "boat_99";
            return root.ToString();
        });
        Assert.False(pass.Succeeded);
        Assert.Contains(pass.Errors, e => e.Contains("pass.json") && e.Contains("boat_99"));

        var order = TestSupport.ConfigWith(GameConfigLoader.PassFile, j =>
        {
            var root = JObject.Parse(j);
            root["milestones"]![1]!["level"] = 5;
            return root.ToString();
        });
        Assert.False(order.Succeeded);
        Assert.Contains(order.Errors, e => e.Contains("pass.json") && e.Contains("ordem crescente"));
    }
}
