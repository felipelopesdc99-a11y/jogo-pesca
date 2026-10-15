using FishingIdle.GameService.Config;
using FishingIdle.GameService.Persistence;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>
/// Back to 5 boats (M24-T42, A-168): a save from the 10-boat model (save version 19) loses boats 6 to 10. Their fragments
/// become fragments of the boat their map drops now (6→1 … 10→5); an assembled one becomes the Moedas and Conchas its levels
/// cost plus the fragments it cost; when it was in use, the player uses the best boat they have. Never twice.
/// </summary>
public sealed class BackToFiveBoatsTests
{
    [Fact]
    public void The_removed_boats_are_kept_in_the_config_only_to_convert_old_saves()
    {
        var config = TestSupport.RealConfig();
        Assert.Equal(2, config.BoatModelVersion);
        var removed = config.Equipment.BoatMigration.RemovedBoats;
        Assert.Equal(new[] { "boat_06", "boat_07", "boat_08", "boat_09", "boat_10" }, removed.Select(b => b.Id));
        Assert.Equal(new[] { "boat_01", "boat_02", "boat_03", "boat_04", "boat_05" }, removed.Select(b => b.FragmentsGoTo));
        Assert.Equal(new[] { 70, 90, 120, 155, 200 }, removed.Select(b => b.FragmentsRequired));
        Assert.All(removed, b => Assert.False(config.TryGetBoat(b.Id, out _)));
    }

    [Fact]
    public void A_version_19_save_with_boats_6_to_10_gets_their_value_back_and_uses_its_best_boat()
    {
        var config = TestSupport.RealConfig();
        var (game, clock, dir) = TestSupport.NewGame(config);
        game.Session.Save.Coins = 1_000;
        game.Session.Save.Shells = 10;
        game.Session.Persist();

        // The same player as a version 19 file (10 boats): boat_06 at Nv.10 in use, boat_08 at Nv.1, fragments of boats 6 and 7.
        var path = Path.Combine(dir, JsonFilePlayerRepository.SaveFileName);
        var json = JObject.Parse(File.ReadAllText(path));
        json["save_version"] = 19;
        json.Remove("boat_model_version");
        json["owned_boat_ids"] = new JArray("boat_06", "boat_08");
        json["boat_levels"] = new JObject { ["boat_06"] = 10, ["boat_08"] = 1 };
        json["boat_fragments"] = new JObject { ["boat_06"] = 5, ["boat_07"] = 30 };
        json["boat_id"] = "boat_06";
        File.WriteAllText(path, json.ToString());

        var reopened = TestSupport.NewGame(TestSupport.RealConfig(), dir, clock).Game;
        var save = reopened.Session.Save;
        Assert.Equal(PlayerSave.CurrentVersion, save.SaveVersion);
        Assert.Equal(2, save.BoatModelVersion);

        // Nothing of boats 6 to 10 is left.
        Assert.DoesNotContain(save.OwnedBoatIds, id => id == "boat_06" || id == "boat_08");
        Assert.DoesNotContain(save.BoatLevels.Keys, id => id == "boat_06" || id == "boat_08");
        Assert.DoesNotContain(save.BoatFragments.Keys, id => id == "boat_06" || id == "boat_07");

        // boat_06 at Nv.10: the 9 levels it went up, at its old price (curve of TD-041); boat_08 at Nv.1 cost no level.
        long coins = 0, shells = 0;
        for (var n = 1; n < 10; n++)
        {
            coins += LevelCurve.Cost(375_000, 1.14, n);
            shells += LevelCurve.Cost(40, 1.03, n);
        }

        Assert.Equal(1_000 + coins, save.Coins);
        Assert.Equal(10 + shells, save.Shells);

        // Fragments: boat_01 gets 5 + 70 (boat_06's) = 75 → assembled (20) + 55 extra; boat_02 gets boat_07's 30 → assembled
        // (25) + 5; boat_03 gets the 120 boat_08 cost → assembled (35) + 85.
        Assert.Contains("boat_01", save.OwnedBoatIds);
        Assert.Contains("boat_02", save.OwnedBoatIds);
        Assert.Contains("boat_03", save.OwnedBoatIds);
        Assert.Equal(55, save.BoatFragments["boat_01"]);
        Assert.Equal(5, save.BoatFragments["boat_02"]);
        Assert.Equal(85, save.BoatFragments["boat_03"]);

        // boat_06 was in use: now the best boat the player has.
        Assert.Equal("boat_03", reopened.Gear.GetGear().BoatId);

        // Opening the game again changes nothing.
        reopened.Session.Persist();
        var again = TestSupport.NewGame(TestSupport.RealConfig(), dir, clock).Game.Session.Save;
        Assert.Equal(save.Coins, again.Coins);
        Assert.Equal(save.Shells, again.Shells);
        Assert.Equal(55, again.BoatFragments["boat_01"]);
        Assert.Equal("boat_03", again.BoatId);
    }

    [Fact]
    public void A_boat_of_today_in_use_stays_in_use_and_a_save_without_boats_6_to_10_changes_nothing()
    {
        var (game, clock, dir) = TestSupport.NewGame(TestSupport.RealConfig());
        TestSupport.GiveBoat(game, "boat_02");
        game.Session.Save.BoatLevels["boat_02"] = 4;
        var coinsBefore = game.Session.Save.Coins;
        game.Session.Persist();

        var path = Path.Combine(dir, JsonFilePlayerRepository.SaveFileName);
        var json = JObject.Parse(File.ReadAllText(path));
        json["save_version"] = 19;
        json.Remove("boat_model_version");
        json["boat_fragments"] = new JObject { ["boat_02"] = 3, ["boat_09"] = 7 };
        File.WriteAllText(path, json.ToString());

        var save = TestSupport.NewGame(TestSupport.RealConfig(), dir, clock).Game.Session.Save;
        Assert.Equal("boat_02", save.BoatId);
        Assert.Equal(4, save.BoatLevels["boat_02"]);
        Assert.Equal(coinsBefore, save.Coins);
        // boat_09's 7 fragments went to boat_04 (map 9 drops boat 4), not enough to assemble it.
        Assert.Equal(7, save.BoatFragments["boat_04"]);
        Assert.DoesNotContain("boat_04", save.OwnedBoatIds);
        Assert.Equal(3, save.BoatFragments["boat_02"]);
    }

    [Fact]
    public void A_broken_boat_migration_is_rejected_in_portuguese()
    {
        var bad = TestSupport.ConfigWith(GameConfigLoader.EquipmentFile, j =>
        {
            var root = JObject.Parse(j);
            root["boat_migration"]!["removed_boats"]![0]!["fragments_go_to"] = "boat_99";
            return root.ToString();
        });
        Assert.False(bad.Succeeded);
        Assert.Contains(bad.Errors, e => e.Contains("boat_migration") && e.Contains("boat_06"));

        var taken = TestSupport.ConfigWith(GameConfigLoader.EquipmentFile, j =>
        {
            var root = JObject.Parse(j);
            root["boats"]![1]!["extra_fragment_maps"]![0]!["map_id"] = "map_02";
            return root.ToString();
        });
        Assert.False(taken.Succeeded);
        Assert.Contains(taken.Errors, e => e.Contains("map_02") && e.Contains("mais de um barco"));
    }
}
