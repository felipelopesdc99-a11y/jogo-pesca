using System.Diagnostics;
using FishingIdle.GameService;
using FishingIdle.GameService.Aquarium;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Crew;
using FishingIdle.GameService.Market;
using FishingIdle.GameService.Persistence;
using FishingIdle.GameService.Ranking;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>
/// M24-T38: opening the game the way the Unity client does (GameRoot: start, the first frame, then the syncs every frame),
/// with saves of every older version, "dirty" like a real player's (rods Nv.10 in the Inventory, on the Market and in an
/// auction, old baits, fish Nv.10 in the Cardume, boats bought before fragments, Dólares paid by the old rule) and three
/// days without opening; and with a brand-new save. Nothing may throw, the save must load (never be set aside) and the
/// opening must be quick.
/// </summary>
public class StartupWithOldSavesTests
{
    // 19:05 in Brasília, three days after TestSupport.StartMs: the Boss Mundial is open on the first frame and closes
    // (and pays) during the frames that follow.
    private static readonly long ReopenAtMs = new DateTimeOffset(2026, 9, 24, 22, 5, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();

    public static IEnumerable<object[]> OldVersions()
    {
        for (var version = 11; version < PlayerSave.CurrentVersion; version++)
        {
            yield return new object[] { version };
        }
    }

    [Theory]
    [MemberData(nameof(OldVersions))]
    public void An_old_dirty_save_opens_three_days_later_and_every_service_keeps_running(int version)
    {
        var (dir, clock) = WriteDirtySave(version);
        clock.UtcNowMs = ReopenAtMs;

        var watch = Stopwatch.StartNew();
        var game = Open(dir, clock);
        FirstFrame(game);
        watch.Stop();
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(2), "Opening a version " + version + " save took " + watch.Elapsed.TotalSeconds + " s.");
        Assert.Equal(SaveLoadStatus.Loaded, game.Session.LoadStatus);

        var run = Stopwatch.StartNew();
        RunFrames(game, clock, frames: 80, stepSeconds: 15);
        run.Stop();
        Assert.True(run.Elapsed < TimeSpan.FromSeconds(10), "80 frames of a version " + version + " save took " + run.Elapsed.TotalSeconds + " s.");

        var save = game.Session.Save;
        Assert.Equal(PlayerSave.CurrentVersion, save.SaveVersion);
        Assert.Empty(SaveValidator.Validate(save));
        Assert.True(save.FisherLevel >= 60);
        Assert.True(save.Dollars >= 60);
        Assert.Contains("boat_02", save.OwnedBoatIds);

        // Closing and opening again (what the owner does after the first run) loads the migrated save as it is.
        game.Fishing.MarkSeen();
        game.Crew.MarkSeen();
        clock.AdvanceSeconds(3600);
        var again = Open(dir, clock);
        FirstFrame(again);
        Assert.Equal(SaveLoadStatus.Loaded, again.Session.LoadStatus);
        RunFrames(again, clock, frames: 5, stepSeconds: 1);
    }

    [Fact]
    public void A_new_save_opens_plays_and_opens_again_three_days_later()
    {
        var dir = TestSupport.NewTempDirectory();
        var clock = new ManualClock(TestSupport.StartMs);

        var watch = Stopwatch.StartNew();
        var game = Open(dir, clock);
        FirstFrame(game);
        watch.Stop();
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(2), "Opening a new save took " + watch.Elapsed.TotalSeconds + " s.");
        Assert.Equal(SaveLoadStatus.NotFound, game.Session.LoadStatus);
        RunFrames(game, clock, frames: 20, stepSeconds: 1);

        // The tutorial's first steps, then fishing, and the game closed for three days.
        game.Tutorial.Skip();
        Assert.True(game.Fishing.StartFishing().Succeeded);
        RunFrames(game, clock, frames: 20, stepSeconds: 5);
        game.Fishing.MarkSeen();
        game.Crew.MarkSeen();
        clock.UtcNowMs = ReopenAtMs;

        var reopen = Stopwatch.StartNew();
        var again = Open(dir, clock);
        FirstFrame(again);
        reopen.Stop();
        Assert.True(reopen.Elapsed < TimeSpan.FromSeconds(2), "Reopening a new save took " + reopen.Elapsed.TotalSeconds + " s.");
        Assert.Equal(SaveLoadStatus.Loaded, again.Session.LoadStatus);
        RunFrames(again, clock, frames: 80, stepSeconds: 15);
        Assert.Empty(SaveValidator.Validate(again.Session.Save));
    }

    // ------------------------------------------------------------------ the old save

    /// <summary>
    /// A player of the current version made "old": progress on every system the old versions had, then the file rewritten
    /// as <paramref name="version"/> (the fields added after it removed, the old values put back).
    /// </summary>
    private static (string Dir, ManualClock Clock) WriteDirtySave(int version)
    {
        var (game, clock, dir) = TestSupport.NewGame(TestSupport.RealConfig());
        var save = game.Session.Save;
        game.Session.DevToolsEnabled = true;
        save.Coins = 50_000_000;
        save.Shells = 50_000;
        save.FisherLevel = 60;
        save.FisherXp = 0;
        save.Dollars = 60;   // Nv.10 to Nv.60 paid by the old rule (A-111)

        // Rods: three assembled (rod_03 ends up in use), all at the old top level (Nv.10).
        TestSupport.GiveRod(game, "rod_01");
        TestSupport.GiveRod(game, "rod_02");
        TestSupport.GiveRod(game, "rod_03");
        TestSupport.GiveBoat(game, "boat_02");
        foreach (var item in save.Inventory.Where(i => i.Kind == InventoryItem.KindRod))
        {
            item.Level = 10;
        }

        // Fish Nv.10: six in the Cardume, the others for the Market and an auction.
        game.DevTools.GiveFish("lambari", null, 9);
        var kept = game.Aquarium.KeepCatches(game.Fishing.GetFishingBox().Select(c => c.CatchId).ToList()).Value.Kept;
        foreach (var fish in save.Aquarium)
        {
            fish.Level = 10;
            fish.Xp = 0;
        }

        for (var position = 1; position <= 6 && position <= kept.Count; position++)
        {
            game.Cardume.SetSlot(position, kept[position - 1].FishId);
        }

        var rods = save.Inventory.Where(i => i.Kind == InventoryItem.KindRod && i.Id != save.EquippedRodItemId && i.RodId != "rod_00_starter").ToList();
        if (rods.Count > 0) game.Market.ListRod(rods[0].Id, 5_000);
        if (rods.Count > 1) game.Auctions.StartAuction(false, rods[1].Id, 5_000);
        if (kept.Count > 6) game.Market.ListFish(kept[6].FishId, 500);
        if (kept.Count > 7) game.Auctions.StartAuction(true, kept[7].FishId, 500);

        // Fishing on, an Expedition on its way, a few Crew members hired (from the version that has the Crew).
        game.Expeditions.Start("exp_30m");
        game.Fishing.StartFishing();
        if (version >= 13)
        {
            game.Crew.Hire("crew_01", CrewBuyMode.Ten);
        }

        game.Fishing.MarkSeen();
        game.Crew.MarkSeen();
        game.Session.Persist();

        var path = Path.Combine(dir, JsonFilePlayerRepository.SaveFileName);
        var json = JObject.Parse(File.ReadAllText(path));
        json["save_version"] = version;
        json["campo_de_uma_versao_antiga"] = 1;   // unknown fields of old files are ignored
        if (version <= 18)
        {
            json.Remove("world_boss");
        }

        if (version <= 17)
        {
            json.Remove("rod_fragments");
            json.Remove("clothing");
        }

        if (version <= 16)
        {
            // The old baits (model 1): charges left of two of them, one in use, and one empty entry.
            json.Remove("bait_model_version");
            json["bait_charges"] = new JObject { ["bait_02"] = 40, ["bait_03"] = 7, ["bait_01"] = 0 };
            json["active_bait_id"] = "bait_02";
        }

        if (version <= 15)
        {
            // Boats were bought before the fragments: one more boat owned and in use.
            json.Remove("boat_fragments");
            json.Remove("pass");
            ((JArray)json["owned_boat_ids"]).Add("boat_04");
            json["boat_id"] = "boat_04";
        }

        if (version <= 14)
        {
            json.Remove("boat_levels");
            json.Remove("rod_level_curve_version");
            json.Remove("fish_level_curve_version");
        }

        if (version <= 13)
        {
            json.Remove("upgrades");
        }

        if (version <= 12)
        {
            json.Remove("crew");
        }

        if (version <= 11)
        {
            json.Remove("fisher_xp_curve_version");
            json.Remove("fisher_xp_total");
        }
        else
        {
            json["fisher_xp_curve_version"] = version <= 15 ? 1 : 2;
        }

        File.WriteAllText(path, json.ToString());
        File.Delete(Path.Combine(dir, JsonFilePlayerRepository.BackupFileName));
        return (dir, clock);
    }

    // ------------------------------------------------------------------ what GameRoot does

    private static LocalGame Open(string dir, ManualClock clock)
    {
        var result = LocalGame.Start(TestSupport.RealConfig(), new JsonFilePlayerRepository(dir), clock, _ => { });
        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        result.Game.Session.DevToolsEnabled = true;
        return result.Game;
    }

    /// <summary>GameRoot.StartGameService and the first Update, then every window the player can open.</summary>
    private static void FirstFrame(LocalGame game)
    {
        Refresh(game);
        game.Tutorial.Get();
        game.Crew.TakeOfflineReport();
        game.Fishing.TakeOfflineReport();
        Frame(game);
        Windows(game);
    }

    private static void RunFrames(LocalGame game, ManualClock clock, int frames, double stepSeconds)
    {
        for (var i = 0; i < frames; i++)
        {
            clock.AdvanceSeconds(stepSeconds);
            Frame(game);
            if (i % 20 == 0)
            {
                Windows(game);
            }
        }

        // What the player clicks first after coming back: the pass rewards and the Expedition report.
        game.Pass.ClaimAll();
        game.Expeditions.AcknowledgeResult();
        Frame(game);
        Windows(game);
    }

    /// <summary>GameRoot.Update: the sync every 0,25 s and the Crew every second.</summary>
    private static void Frame(LocalGame game)
    {
        game.Maps.Update();
        game.Fishing.Sync();
        game.Crew.TakeOfflineReport();
        game.Fishing.TakeOfflineReport();
        game.Expeditions.Update();
        game.Expeditions.PendingResult();
        game.Arena.Update();
        game.Tutorial.Get();
        game.WorldBoss.Update();
        game.WorldBoss.GetWorldBoss();
        game.Market.Update();
        game.Crew.Sync();
        Refresh(game);
    }

    /// <summary>GameRoot.Refresh.</summary>
    private static void Refresh(LocalGame game)
    {
        game.Player.GetPlayer();
        game.Fishing.GetStatus();
        game.Maps.GetTravel();
        game.Gear.GetGear();
        game.Clothing.GetClothing();
        game.Vip.GetVip();
        game.Pass.GetPass();
        game.Crew.GetCrew(CrewBuyMode.One);
    }

    /// <summary>What each window asks the service for when it opens.</summary>
    private static void Windows(LocalGame game)
    {
        game.Fishing.GetFishingBox();
        foreach (AquariumSort sort in Enum.GetValues(typeof(AquariumSort)))
        {
            game.Aquarium.GetAquarium(sort);
        }

        game.Profile.GetProfile();
        game.Cardume.GetCardume();
        game.Maps.GetMaps();
        game.Shop.GetShop();
        game.Expeditions.GetExpeditions();
        game.Arena.GetArena();
        game.Market.GetMarket();
        game.Market.Search(new MarketQuery());
        game.Market.GetFilterOptions();
        game.Market.GetSellCandidates();
        game.Auctions.GetAuctions();
        game.CurrencyTrade.GetCurrencyTrade();
        foreach (RankingCategory category in Enum.GetValues(typeof(RankingCategory)))
        {
            game.Ranking.GetRanking(category);
        }

        game.Upgrades.GetUpgrades();
        foreach (CrewBuyMode mode in Enum.GetValues(typeof(CrewBuyMode)))
        {
            game.Crew.GetCrew(mode);
        }

        game.Pass.GetPass();
        game.WorldBoss.GetWorldBoss();
        game.Clothing.GetClothing();
        game.Vip.GetVip();
    }
}
