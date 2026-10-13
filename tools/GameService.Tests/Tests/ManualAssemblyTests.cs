using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Persistence;
using FishingIdle.GameService.Shop;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>
/// M24-T58 (A-180): boats and rods are assembled by the player with "Montar", each one asking for the one before it; maps
/// open only by the Fisher level; the missions count the manual assembly and the level; old saves keep what they had.
/// </summary>
public sealed class ManualAssemblyTests
{
    [Fact]
    public void Fishing_never_assembles_a_boat_or_a_rod_by_itself()
    {
        var config = TestSupport.ConfigWith(GameConfigLoader.EquipmentFile, j => j.Replace("\"fragment_drop_chance\": 0.001", "\"fragment_drop_chance\": 1.0"), certainCatch: true);
        Assert.True(config.Succeeded, string.Join("\n", config.Errors));
        var (game, clock, dir) = TestSupport.NewGame(config.Config);
        game.Fishing.StartFishing();

        // 60 fish pulled out: more than any boat and rod ask for, and nothing new is owned.
        TestSupport.PlayFor(game, clock, 60 * config.Config.Fishing.OnlineCycleSeconds, config.Config.Fishing.OnlineCycleSeconds);
        var gear = game.Gear.GetGear();
        Assert.All(gear.Boats.Where(b => b.Tier > 0), b => Assert.False(b.Owned));
        Assert.All(gear.Boats.Where(b => b.Tier > 0), b => Assert.True(b.Fragments >= b.FragmentsRequired));
        Assert.Equal("boat_00", gear.BoatId);
        Assert.Single(game.Profile.GetProfile().Inventory);

        // Only boat 1 can be assembled: each of the others waits for the one before it.
        Assert.Equal(ServiceError.None, gear.Boats.Single(b => b.BoatId == "boat_01").AssembleBlocker);
        Assert.All(gear.Boats.Where(b => b.Tier > 1), b => Assert.Equal(ServiceError.BoatNeedsPrevious, b.AssembleBlocker));
        foreach (var id in new[] { "boat_01", "boat_02", "boat_03", "boat_04", "boat_05" })
        {
            Assert.True(game.Gear.AssembleBoat(id).Succeeded, id);
        }

        Assert.Equal("boat_00", game.Gear.GetGear().BoatId);   // not put in use by itself

        // Reopening the save (TD-050) keeps everything and still assembles nothing more.
        game.Session.Persist();
        var owned = game.Session.Save.OwnedBoatIds.Count;
        var fragments = BoatFragmentRules.Have(game.Session.Save, "boat_05");
        var reopened = TestSupport.NewGame(config.Config, dir, clock).Game;
        Assert.Equal(owned, reopened.Session.Save.OwnedBoatIds.Count);
        Assert.Equal(fragments, BoatFragmentRules.Have(reopened.Session.Save, "boat_05"));
    }

    [Fact]
    public void An_old_save_with_enough_fragments_opens_with_them_kept_and_what_it_owned_stays()
    {
        var (game, clock, dir) = TestSupport.NewGame(TestSupport.RealConfig());
        TestSupport.GiveBoat(game, "boat_01");
        TestSupport.GiveRod(game, "rod_01");
        var save = game.Session.Save;
        save.BoatFragments["boat_02"] = 999;
        save.RodFragments["rod_02"] = 999;
        game.Session.Persist();

        var reopened = TestSupport.NewGame(TestSupport.RealConfig(), dir, clock).Game;
        var after = reopened.Session.Save;
        Assert.Contains("boat_01", after.OwnedBoatIds);
        Assert.DoesNotContain("boat_02", after.OwnedBoatIds);
        Assert.True(RodFragmentRules.Owns(after, "rod_01"));
        Assert.False(RodFragmentRules.Owns(after, "rod_02"));
        Assert.Equal(999, BoatFragmentRules.Have(after, "boat_02"));
        Assert.Equal(999, RodFragmentRules.Have(after, "rod_02"));
        Assert.Equal("boat_01", after.BoatId);
        Assert.Equal("rod_01", after.EquippedRodItem().RodId);
    }

    [Fact]
    public void The_missions_count_the_manual_assembly_and_the_map_opened_by_level()
    {
        var (game, _, _) = TestSupport.NewGame(TestSupport.RealConfig());
        var config = game.Session.Config;
        var save = game.Session.Save;
        long Current(string id) => game.Missions.GetMissions().Missions.Single(m => m.Id == id).Current;

        // mission_016: rod_01; mission_026: boat_01; mission_017: map_02.
        config.TryGetRod("rod_01", out var rod);
        config.TryGetBoat("boat_01", out var boat);
        RodFragmentRules.Add(config, save, rod, rod.FragmentsRequired, 0);
        BoatFragmentRules.Add(config, save, boat, boat.FragmentsRequired);
        Assert.Equal(0, Current("mission_016"));
        Assert.Equal(0, Current("mission_026"));
        Assert.True(game.Shop.AssembleRod("rod_01").Succeeded);
        Assert.True(game.Gear.AssembleBoat("boat_01").Succeeded);
        Assert.Equal(1, Current("mission_016"));
        Assert.Equal(1, Current("mission_026"));

        // Map 2 opens at its level with the Starter Rod in use: the mission counts it.
        Assert.Equal(0, Current("mission_017"));
        save.FisherLevel = 10;
        Assert.Equal("rod_00_starter", save.EquippedRodItem().RodId);
        Assert.Equal(1, Current("mission_017"));
        Assert.True(game.Maps.TravelTo("map_02").Succeeded);
    }
}
