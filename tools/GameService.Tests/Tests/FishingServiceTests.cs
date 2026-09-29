using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using Xunit;

namespace FishingIdle.GameService.Tests;

public sealed class FishingServiceTests
{
    [Fact]
    public void A_new_player_starts_on_map_1_with_the_starter_rod_and_nothing_else()
    {
        var (game, _, _) = TestSupport.NewGame();
        var player = game.Player.GetPlayer();

        Assert.Equal(1, player.FisherLevel);
        Assert.Equal(0, player.Coins);
        Assert.Equal("Lago Sereno", player.MapName);
        Assert.Equal("Vara Inicial", player.RodName);
        Assert.Empty(game.Fishing.GetFishingBox());
        Assert.False(game.Fishing.GetStatus().IsFishing);
    }

    [Fact]
    public void Nothing_is_caught_before_fishing_starts()
    {
        var (game, clock, _) = TestSupport.NewGame();
        clock.AdvanceSeconds(600);

        Assert.Empty(game.Fishing.Sync().NewCatches);
    }

    [Fact]
    public void One_catch_arrives_every_30_seconds_while_fishing()
    {
        var (game, clock, _) = TestSupport.NewGame();
        Assert.True(game.Fishing.StartFishing().Succeeded);

        Assert.Empty(TestSupport.PlayFor(game, clock, 29));
        Assert.Single(TestSupport.PlayFor(game, clock, 1));
        var tenMinutes = TestSupport.PlayFor(game, clock, 600);

        Assert.Equal(20, tenMinutes.Count);
        Assert.Equal(21, game.Fishing.GetFishingBox().Count);
    }

    [Fact]
    public void Repeated_syncs_cannot_produce_extra_fish()
    {
        var (game, clock, _) = TestSupport.NewGame();
        game.Fishing.StartFishing();
        clock.AdvanceSeconds(95);

        var first = game.Fishing.Sync().NewCatches.Count;
        var again = Enumerable.Range(0, 50).Sum(_ => game.Fishing.Sync().NewCatches.Count);

        Assert.Equal(3, first);
        Assert.Equal(0, again);
    }

    [Fact]
    public void Starting_twice_is_refused_and_does_not_reset_the_cycle()
    {
        var (game, clock, _) = TestSupport.NewGame();
        game.Fishing.StartFishing();
        clock.AdvanceSeconds(20);
        game.Fishing.Sync();

        var second = game.Fishing.StartFishing();

        Assert.False(second.Succeeded);
        Assert.Equal(ServiceError.AlreadyFishing, second.Error);
        Assert.Equal("Você já está pescando.", second.ErrorMessage);
        Assert.Single(TestSupport.PlayFor(game, clock, 10));
    }

    [Fact]
    public void Stopping_settles_completed_cycles_and_then_nothing_more_arrives()
    {
        var (game, clock, _) = TestSupport.NewGame();
        game.Fishing.StartFishing();
        clock.AdvanceSeconds(61);

        var stop = game.Fishing.StopFishing();
        clock.AdvanceSeconds(300);

        Assert.Equal(2, stop.Value.NewCatches.Count);
        Assert.Empty(game.Fishing.Sync().NewCatches);
        Assert.False(game.Fishing.GetStatus().IsFishing);
    }

    [Fact]
    public void Status_reports_the_countdown_of_the_current_cycle()
    {
        var (game, clock, _) = TestSupport.NewGame();
        game.Fishing.StartFishing();
        clock.AdvanceSeconds(40);
        game.Fishing.Sync();

        var status = game.Fishing.GetStatus();

        Assert.True(status.IsFishing);
        Assert.Equal(TestSupport.StartMs + 60_000, status.NextCatchAtMs);
        Assert.InRange(status.CycleProgress, 0.33, 0.34);
    }

    [Fact]
    public void Time_the_game_was_closed_counts_as_offline_fishing_at_60_seconds()
    {
        var saveDir = TestSupport.NewTempDirectory();
        var clock = new ManualClock(TestSupport.StartMs);
        var (game, _, _) = TestSupport.NewGame(saveDir: saveDir, clock: clock);
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 60);
        game.Fishing.MarkSeen(); // the client does this on quit

        clock.AdvanceSeconds(3 * 3600); // three hours closed
        var (reopened, _, _) = TestSupport.NewGame(saveDir: saveDir, clock: clock);
        var report = reopened.Fishing.TakeOfflineReport();

        Assert.Equal(2 + 180, reopened.Fishing.GetFishingBox().Count);
        Assert.Equal(180, report.Update.NewCatches.Count);
        Assert.False(report.Capped);
        Assert.Null(reopened.Fishing.TakeOfflineReport());
        Assert.True(reopened.Fishing.GetStatus().IsFishing); // still fishing, from now on
        Assert.Single(TestSupport.PlayFor(reopened, clock, 30));
    }

    [Fact]
    public void Offline_fishing_stops_accumulating_after_24_hours()
    {
        var saveDir = TestSupport.NewTempDirectory();
        var clock = new ManualClock(TestSupport.StartMs);
        var (game, _, _) = TestSupport.NewGame(saveDir: saveDir, clock: clock);
        game.Fishing.StartFishing();
        game.Fishing.MarkSeen();

        clock.AdvanceSeconds(3 * 24 * 3600);
        var (reopened, _, _) = TestSupport.NewGame(saveDir: saveDir, clock: clock);
        var report = reopened.Fishing.TakeOfflineReport();

        Assert.Equal(1440, reopened.Fishing.GetFishingBox().Count);
        Assert.True(report.Capped);
        Assert.Equal(24 * 3600 * 1000L, report.CountedMs);
    }

    [Fact]
    public void Reopening_twice_does_not_credit_the_same_offline_time_again()
    {
        var saveDir = TestSupport.NewTempDirectory();
        var clock = new ManualClock(TestSupport.StartMs);
        var (game, _, _) = TestSupport.NewGame(saveDir: saveDir, clock: clock);
        game.Fishing.StartFishing();
        game.Fishing.MarkSeen();
        clock.AdvanceSeconds(3600);

        var (first, _, _) = TestSupport.NewGame(saveDir: saveDir, clock: clock);
        var (second, _, _) = TestSupport.NewGame(saveDir: saveDir, clock: clock);

        Assert.Equal(60, first.Fishing.GetFishingBox().Count);
        Assert.Equal(60, second.Fishing.GetFishingBox().Count);
        Assert.Null(second.Fishing.TakeOfflineReport());
    }

    [Fact]
    public void Stopped_fishing_earns_nothing_offline()
    {
        var saveDir = TestSupport.NewTempDirectory();
        var clock = new ManualClock(TestSupport.StartMs);
        var (game, _, _) = TestSupport.NewGame(saveDir: saveDir, clock: clock);
        game.Fishing.StartFishing();
        game.Fishing.StopFishing();
        clock.AdvanceSeconds(5 * 3600);

        var (reopened, _, _) = TestSupport.NewGame(saveDir: saveDir, clock: clock);

        Assert.Empty(reopened.Fishing.GetFishingBox());
        Assert.Null(reopened.Fishing.TakeOfflineReport());
    }

    [Fact]
    public void A_sleeping_pc_while_open_is_offline_time_not_online_time()
    {
        var (game, clock, _) = TestSupport.NewGame();
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 30);

        clock.AdvanceSeconds(24 * 3600);
        var afterJump = game.Fishing.Sync().NewCatches.Count;
        var report = game.Fishing.TakeOfflineReport();

        Assert.Equal(0, afterJump); // nothing counted at the 30 s online rate
        Assert.Equal(1440, report.Update.NewCatches.Count); // 24 h at the 60 s offline rate
        Assert.Equal(1 + 1440, game.Fishing.GetFishingBox().Count);
    }

    [Fact]
    public void Selling_pays_the_listed_price_once_and_the_fish_leave_the_box()
    {
        var (game, clock, _) = TestSupport.NewGame();
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 150);
        var box = game.Fishing.GetFishingBox();
        var ids = box.Take(3).Select(c => c.CatchId).ToList();
        var expected = box.Take(3).Sum(c => c.SalePriceCoins);

        var sale = game.Fishing.SellCatches(ids);
        var again = game.Fishing.SellCatches(ids);

        Assert.True(sale.Succeeded);
        Assert.Equal(expected, sale.Value.CoinsGained);
        Assert.Equal(expected, game.Player.GetPlayer().Coins);
        Assert.Equal(box.Count - 3, game.Fishing.GetFishingBox().Count);
        Assert.False(again.Succeeded);
        Assert.Equal(ServiceError.CatchNotFound, again.Error);
        Assert.Equal(expected, game.Player.GetPlayer().Coins);
    }

    [Fact]
    public void A_sale_with_one_unknown_id_changes_nothing()
    {
        var (game, clock, _) = TestSupport.NewGame();
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 60);
        var ids = game.Fishing.GetFishingBox().Select(c => c.CatchId).Append(9999).ToList();

        var sale = game.Fishing.SellCatches(ids);

        Assert.False(sale.Succeeded);
        Assert.Equal(2, game.Fishing.GetFishingBox().Count);
        Assert.Equal(0, game.Player.GetPlayer().Coins);
    }

    [Fact]
    public void The_first_catch_of_a_species_is_marked_new_and_the_next_one_is_not()
    {
        var (game, clock, _) = TestSupport.NewGame();
        game.Fishing.StartFishing();
        var catches = TestSupport.PlayFor(game, clock, 3000);

        foreach (var group in catches.GroupBy(c => c.SpeciesId))
        {
            var ordered = group.OrderBy(c => c.CatchId).ToList();
            Assert.True(ordered[0].IsNewSpecies);
            Assert.All(ordered.Skip(1), c => Assert.False(c.IsNewSpecies));
        }

        Assert.Equal(catches.Select(c => c.SpeciesId).Distinct().Count(), game.Player.GetPlayer().SpeciesDiscovered);
    }

    [Fact]
    public void A_personal_record_reports_the_previous_best_size_of_the_species()
    {
        var (game, clock, _) = TestSupport.NewGame();
        game.Fishing.StartFishing();
        var catches = TestSupport.PlayFor(game, clock, 6000);
        var records = catches.Where(c => c.IsPersonalRecord).ToList();

        Assert.NotEmpty(records);
        foreach (var record in records)
        {
            var earlierBest = catches.Where(c => c.SpeciesId == record.SpeciesId && c.CatchId < record.CatchId).Max(c => c.SizeCm);
            Assert.Equal(earlierBest, record.PreviousRecordCm, 3);
            Assert.True(record.SizeCm > record.PreviousRecordCm);
        }

        Assert.All(catches.Where(c => !c.IsPersonalRecord), c => Assert.Equal(0, c.PreviousRecordCm));
    }

    [Fact]
    public void Level_10_takes_roughly_two_hours_of_online_fishing()
    {
        // GDD section 17: Lv.1→10 around 2 hours online (~240 catches), on average.
        var levels = new List<int>();
        for (var run = 0; run < 5; run++)
        {
            var (game, clock, _) = TestSupport.NewGame();
            game.Fishing.StartFishing();
            TestSupport.PlayFor(game, clock, 2 * 3600, stepSeconds: 30);
            levels.Add(game.Player.GetPlayer().FisherLevel);
        }

        Assert.InRange(levels.Average(), 9, 11);
    }

    [Fact]
    public void Leveling_up_is_reported_and_xp_carries_over()
    {
        var (game, clock, _) = TestSupport.NewGame();
        game.Fishing.StartFishing();
        var levels = new List<int>();
        for (var i = 0; i < 20; i++)
        {
            clock.AdvanceSeconds(30);
            levels.AddRange(game.Fishing.Sync().LevelsReached);
        }

        var player = game.Player.GetPlayer();
        Assert.Contains(2, levels);
        Assert.Equal(player.FisherLevel, levels.Max());
        Assert.InRange(player.FisherXp, 0, player.FisherXpToNext - 1);
    }

    [Fact]
    public void Applying_a_new_cycle_time_takes_effect_on_the_next_cycle()
    {
        var (game, clock, _) = TestSupport.NewGame();
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 45);

        var faster = TestSupport.ConfigWith(GameConfigLoader.ProgressionFile,
            json => json.Replace("\"online_cycle_seconds\": 30", "\"online_cycle_seconds\": 5"));
        game.Session.ReplaceConfig(faster.Config);

        Assert.Equal(6, TestSupport.PlayFor(game, clock, 30).Count);
        Assert.Equal(5, game.Fishing.GetStatus().CycleSeconds);
    }

    [Fact]
    public void Protected_catches_are_flagged_for_the_bulk_sale_confirmation()
    {
        var (game, clock, _) = TestSupport.NewGame();
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 6 * 3600, stepSeconds: 30);
        var box = game.Fishing.GetFishingBox();

        var preview = game.Fishing.PreviewSale(box.Select(c => c.CatchId).ToList());

        Assert.Equal(box.Count, preview.Count);
        Assert.Equal(box.Sum(c => c.SalePriceCoins), preview.TotalCoins);
        Assert.All(preview.ProtectedCatches, c => Assert.Equal("exceptional", c.SizeCategoryId));
        Assert.Equal(box.Count(c => c.SizeCategoryId == "exceptional"), preview.ProtectedCatches.Count);
    }
}
