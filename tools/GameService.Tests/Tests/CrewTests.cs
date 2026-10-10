using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Crew;
using FishingIdle.GameService.Persistence;
using FishingIdle.Texts;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>
/// The automatic Crew (M24-T05, A-154, TD-039): prices in closed form (×1, ×10, ×100, Máx), unlocks, quantity and
/// fleet milestones, income with the game open and closed (100% then 50%, with a cap), a clock that goes back, the
/// save migration v12 → v13, Fisher XP in the total, and never a fish.
/// </summary>
public sealed class CrewTests
{
    private static CrewMemberConfig Member(GameConfig config, int index) => config.Crew.Members[index];

    private static string Id(GameConfig config, int index) => config.Crew.Members[index].Id;

    private static double Rate(LocalGame game) => CrewRules.CoinsPerSecond(game.Session.Config, game.Session.Save.Crew);

    // ------------------------------------------------------------------ config

    [Fact]
    public void The_crew_has_ten_members_in_portuguese_with_growing_prices_and_income()
    {
        var config = TestSupport.RealConfig();
        var members = config.Crew.Members;
        Assert.Equal(10, members.Count);
        Assert.Equal("Ajudante da Isca", members[0].DisplayName);
        Assert.Equal("Canoeiros", members[1].PluralName);
        Assert.Equal("Navio-Fábrica", members[9].DisplayName);
        for (var i = 0; i < members.Count; i++)
        {
            Assert.InRange(members[i].CostGrowth, 1.07, 1.10);
            if (i > 0)
            {
                Assert.InRange(members[i].BaseCost / (double)members[i - 1].BaseCost, 8.0, 13.0);
                // ~4,6× each since the Upgrades (A-155) recalibrated the income; ~6,5× before.
                Assert.InRange(members[i].CoinsPerSecond / members[i - 1].CoinsPerSecond, 4.0, 7.5);
                Assert.True(members[i].CostGrowth <= members[i - 1].CostGrowth, "the expensive ones grow slower");
            }
        }
    }

    [Fact]
    public void A_cost_growth_of_one_or_less_is_refused()
    {
        var result = TestSupport.ConfigWith(GameConfigLoader.CrewFile, j => j.Replace("\"cost_growth\": 1.1,", "\"cost_growth\": 1.0,"));
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("crew.json") && e.Contains("cost_growth"));
    }

    [Fact]
    public void Milestones_out_of_order_are_refused()
    {
        var result = TestSupport.ConfigWith(GameConfigLoader.CrewFile, j =>
        {
            var root = JObject.Parse(j);
            root["milestones"]!["counts"] = new JArray(50, 25);
            return root.ToString();
        });
        Assert.False(result.Succeeded);
    }

    // ------------------------------------------------------------------ prices

    [Fact]
    public void The_price_of_n_units_is_the_closed_form_of_the_geometric_series()
    {
        var member = Member(TestSupport.RealConfig(), 0);
        var b = member.BaseCost;
        var r = member.CostGrowth;

        Assert.Equal(b, CrewRules.Cost(member, 0, 1));
        Assert.Equal((long)Math.Round(b * Math.Pow(r, 7)), CrewRules.Cost(member, 7, 1));
        Assert.Equal((long)Math.Round(b * (Math.Pow(r, 10) - 1) / (r - 1)), CrewRules.Cost(member, 0, 10));
        Assert.Equal((long)Math.Round(b * Math.Pow(r, 20) * (Math.Pow(r, 100) - 1) / (r - 1)), CrewRules.Cost(member, 20, 100));

        // Ten at once costs what ten single purchases cost (up to the rounding of each one).
        long oneByOne = 0;
        for (var k = 0; k < 10; k++)
        {
            oneByOne += CrewRules.Cost(member, 30 + k, 1);
        }

        Assert.InRange(CrewRules.Cost(member, 30, 10) - oneByOne, -10, 10);
    }

    [Fact]
    public void Max_buys_exactly_what_the_coins_pay()
    {
        var config = TestSupport.RealConfig();
        foreach (var member in config.Crew.Members)
        {
            foreach (var coins in new[] { 0L, member.BaseCost - 1, member.BaseCost, 1_000_000L, 987_654_321_000L, long.MaxValue })
            {
                foreach (var owned in new[] { 0L, 13L, 250L })
                {
                    var n = CrewRules.MaxAffordable(member, owned, coins);
                    if (n > 0)
                    {
                        Assert.True(CrewRules.Cost(member, owned, n) <= coins);
                    }

                    if (n < CrewRules.MaxUnitsPerPurchase)
                    {
                        Assert.False(CrewRules.CanPay(CrewRules.Cost(member, owned, n + 1), coins));
                    }
                }
            }
        }
    }

    [Fact]
    public void A_price_too_big_for_a_number_is_unaffordable_and_never_wraps()
    {
        var member = Member(TestSupport.RealConfig(), 9);
        Assert.Equal(CrewRules.Unaffordable, CrewRules.Cost(member, 5_000, 1));
        Assert.Equal(CrewRules.Unaffordable, CrewRules.Cost(member, 0, 1_000_000));
        Assert.Equal(0, CrewRules.MaxAffordable(member, 5_000, long.MaxValue));

        var (game, _, _) = TestSupport.NewGame();
        var save = game.Session.Save;
        save.Coins = long.MaxValue;
        foreach (var m in game.Session.Config.Crew.Members)
        {
            save.Crew.Units[m.Id] = 5_000;
        }

        var refused = game.Crew.Hire(member.Id, CrewBuyMode.One);
        Assert.Equal(ServiceError.NotEnoughCoins, refused.Error);
        Assert.Equal(long.MaxValue, save.Coins);
        Assert.Equal(CrewRules.Unaffordable, game.Crew.GetCrew(CrewBuyMode.One).Members[9].BuyCost);
    }

    [Fact]
    public void Hiring_one_ten_a_hundred_and_max_charges_the_closed_form_price()
    {
        var (game, _, _) = TestSupport.NewGame();
        var config = game.Session.Config;
        var save = game.Session.Save;
        var first = Member(config, 0);
        save.Coins = 100_000_000;

        var one = game.Crew.Hire(first.Id, CrewBuyMode.One);
        Assert.True(one.Succeeded);
        Assert.Equal(first.BaseCost, one.Value.CoinsSpent);
        Assert.Equal(100_000_000 - first.BaseCost, save.Coins);

        var expectedTen = CrewRules.Cost(first, 1, 10);
        Assert.Equal(expectedTen, game.Crew.GetCrew(CrewBuyMode.Ten).Members[0].BuyCost);
        var ten = game.Crew.Hire(first.Id, CrewBuyMode.Ten);
        Assert.Equal(expectedTen, ten.Value.CoinsSpent);
        Assert.Equal(11, save.Crew.UnitsOf(first.Id));

        var expectedHundred = CrewRules.Cost(first, 11, 100);
        if (expectedHundred <= save.Coins)
        {
            Assert.Equal(expectedHundred, game.Crew.Hire(first.Id, CrewBuyMode.Hundred).Value.CoinsSpent);
            Assert.Equal(111, save.Crew.UnitsOf(first.Id));
        }
        else
        {
            Assert.Equal(ServiceError.NotEnoughCoins, game.Crew.Hire(first.Id, CrewBuyMode.Hundred).Error);
        }

        var second = Member(config, 1);
        var before = save.Coins;
        var owned = save.Crew.UnitsOf(second.Id);
        var expectedMax = CrewRules.MaxAffordable(second, owned, before);
        Assert.Equal(expectedMax, game.Crew.GetCrew(CrewBuyMode.Max).Members[1].BuyAmount);
        var max = game.Crew.Hire(second.Id, CrewBuyMode.Max);
        Assert.True(max.Succeeded);
        Assert.Equal(expectedMax, max.Value.Bought);
        Assert.Equal(before - CrewRules.Cost(second, owned, expectedMax), save.Coins);
        Assert.True(CrewRules.Cost(second, owned + expectedMax, 1) > save.Coins);
    }

    [Fact]
    public void Without_the_coins_nothing_is_hired()
    {
        var (game, _, _) = TestSupport.NewGame();
        var save = game.Session.Save;
        var first = Member(game.Session.Config, 0);
        save.Coins = first.BaseCost - 1;

        Assert.Equal(ServiceError.NotEnoughCoins, game.Crew.Hire(first.Id, CrewBuyMode.One).Error);
        Assert.Equal(ServiceError.NotEnoughCoins, game.Crew.Hire(first.Id, CrewBuyMode.Max).Error);
        Assert.Equal(ServiceError.NotEnoughCoins, game.Crew.GetCrew(CrewBuyMode.One).Members[0].BuyBlocker);
        Assert.Equal(first.BaseCost - 1, save.Coins);
        Assert.Equal(0, save.Crew.UnitsOf(first.Id));
        Assert.Equal(ServiceError.CrewMemberNotFound, game.Crew.Hire("crew_99", CrewBuyMode.One).Error);
    }

    // ------------------------------------------------------------------ unlocks and milestones

    [Fact]
    public void The_first_member_is_open_from_the_start_and_each_next_one_needs_ten_of_the_previous()
    {
        var (game, _, _) = TestSupport.NewGame();
        var config = game.Session.Config;
        var save = game.Session.Save;
        save.Coins = 1_000_000_000;
        var need = config.Crew.UnlockPreviousCount;
        Assert.Equal(10, need);

        var view = game.Crew.GetCrew(CrewBuyMode.One);
        Assert.True(view.Members[0].Unlocked);
        Assert.False(view.Members[1].Unlocked);
        Assert.Equal(ServiceError.CrewMemberLocked, view.Members[1].BuyBlocker);
        Assert.Equal(need, view.Members[1].UnlockAfterCount);
        Assert.Equal("Ajudantes da Isca", view.Members[1].UnlockAfterNamePlural);
        Assert.Equal(ServiceError.CrewMemberLocked, game.Crew.Hire(Id(config, 1), CrewBuyMode.One).Error);

        ServiceResult<CrewHireResult> nine = null;
        for (var i = 1; i < need; i++)
        {
            nine = game.Crew.Hire(Id(config, 0), CrewBuyMode.One);
        }

        Assert.Equal(need - 1, save.Crew.UnitsOf(Id(config, 0)));
        Assert.Null(nine.Value.UnlockedName);
        Assert.Equal(ServiceError.CrewMemberLocked, game.Crew.Hire(Id(config, 1), CrewBuyMode.One).Error);

        var tenth = game.Crew.Hire(Id(config, 0), CrewBuyMode.One);
        Assert.Equal("Canoeiro", tenth.Value.UnlockedName);
        Assert.True(game.Crew.Hire(Id(config, 1), CrewBuyMode.One).Succeeded);
        Assert.False(game.Crew.GetCrew(CrewBuyMode.One).Members[2].Unlocked);
        Assert.Equal("Libera com 10 Canoeiros (você tem 1)", GameTexts.Crew.LockedLine(10, "Canoeiros", Format.Number(1)));
    }

    [Fact]
    public void Each_quantity_milestone_doubles_that_members_income()
    {
        var config = TestSupport.RealConfig();
        var member = Member(config, 2);
        var counts = config.Crew.Milestones.Counts;
        Assert.Equal(new[] { 25, 50, 100, 150, 200, 300, 400 }, counts);

        Assert.Equal(member.CoinsPerSecond * 24, CrewRules.MemberCoinsPerSecond(config, member, 24, 1.0), 9);
        Assert.Equal(member.CoinsPerSecond * 25 * 2, CrewRules.MemberCoinsPerSecond(config, member, 25, 1.0), 9);
        Assert.Equal(member.CoinsPerSecond * 49 * 2, CrewRules.MemberCoinsPerSecond(config, member, 49, 1.0), 9);
        Assert.Equal(member.CoinsPerSecond * 50 * 4, CrewRules.MemberCoinsPerSecond(config, member, 50, 1.0), 9);
        Assert.Equal(member.CoinsPerSecond * 400 * 128, CrewRules.MemberCoinsPerSecond(config, member, 400, 1.0), 6);
        Assert.Equal(member.CoinsPerSecond * 999 * 128, CrewRules.MemberCoinsPerSecond(config, member, 999, 1.0), 6);
        Assert.Equal(50, CrewRules.NextMilestone(config.Crew.Milestones, 25));
        Assert.Equal(0, CrewRules.NextMilestone(config.Crew.Milestones, 400));
    }

    [Fact]
    public void The_fleet_milestone_doubles_everyone_when_every_member_reaches_it()
    {
        var (game, _, _) = TestSupport.NewGame();
        var config = game.Session.Config;
        var crew = game.Session.Save.Crew;
        foreach (var m in config.Crew.Members.Take(9))
        {
            crew.Units[m.Id] = 30;
        }

        crew.Units[Id(config, 9)] = 24;
        Assert.Equal(0, CrewRules.FleetMilestonesReached(config, crew));
        var without = Rate(game);
        var view = game.Crew.GetCrew(CrewBuyMode.One);
        Assert.Equal(25, view.FleetNextMilestone);
        Assert.Equal(9, view.FleetMembersReady);

        game.Session.Save.Coins = long.MaxValue / 2;
        var hire = game.Crew.Hire(Id(config, 9), CrewBuyMode.One);
        Assert.Equal(25, hire.Value.FleetMilestoneReached);
        Assert.Equal(25, hire.Value.MilestoneReached);
        Assert.Equal(1, CrewRules.FleetMilestonesReached(config, crew));

        // Everyone ×2 for the fleet; the last member also ×2 for its own 25 and one unit more.
        var last = Member(config, 9);
        var expected = (without - last.CoinsPerSecond * 24 + last.CoinsPerSecond * 25 * 2) * 2;
        Assert.Equal(expected, Rate(game), 6);
    }

    // ------------------------------------------------------------------ income

    [Fact]
    public void With_the_game_open_the_income_is_credited_by_time()
    {
        var (game, clock, _) = TestSupport.NewGame();
        var config = game.Session.Config;
        var save = game.Session.Save;
        save.Crew.Units[Id(config, 3)] = 30;
        var rate = Rate(game);
        var coins = save.Coins;

        for (var i = 0; i < 600; i++)
        {
            clock.AdvanceSeconds(1);
            game.Crew.Sync();
        }

        Assert.InRange(save.Coins - coins, (long)(rate * 600) - 1, (long)(rate * 600) + 1);
        Assert.Null(game.Crew.TakeOfflineReport());

        // Syncing again at the same moment credits nothing more.
        var now = save.Coins;
        game.Crew.Sync();
        game.Crew.Sync();
        Assert.Equal(now, save.Coins);
    }

    [Fact]
    public void Fractions_of_a_coin_are_kept_until_they_make_a_whole_one()
    {
        var (game, clock, _) = TestSupport.NewGame();
        var config = game.Session.Config;
        var save = game.Session.Save;
        save.Coins = Member(config, 0).BaseCost;
        Assert.True(game.Crew.Hire(Id(config, 0), CrewBuyMode.One).Succeeded);
        var rate = Rate(game);
        Assert.True(rate < 1, "one Ajudante da Isca earns a fraction of a coin per second");

        // Not a whole number of coins at the end, so the floor is not at the mercy of the last decimal.
        var seconds = (int)Math.Ceiling(3 / rate) + 7;
        for (var i = 0; i < seconds; i++)
        {
            clock.AdvanceSeconds(1);
            game.Crew.Sync();
        }

        Assert.Equal((long)Math.Floor(rate * seconds), save.Coins);
    }

    [Fact]
    public void Closed_the_crew_earns_in_full_for_two_hours_then_half_until_ten_hours()
    {
        var config = TestSupport.RealConfig();
        Assert.Equal(2, config.Crew.Offline.FullRateHours);
        Assert.Equal(0.5, config.Crew.Offline.ReducedRate);
        Assert.Equal(10, config.Crew.Offline.MaxHours);

        foreach (var hours in new[] { 1.0, 2.0, 5.0, 10.0, 30.0 })
        {
            var (game, clock, dir) = TestSupport.NewGame();
            var save = game.Session.Save;
            save.Crew.Units[Id(config, 4)] = 60;
            game.Crew.MarkSeen();
            var rate = Rate(game);
            var coins = save.Coins;

            clock.AdvanceSeconds(hours * 3600);
            var reopened = TestSupport.NewGame(saveDir: dir, clock: clock).Game;

            var effective = Math.Min(hours, 2.0) * 3600 + Math.Max(0, Math.Min(hours, 10.0) - 2.0) * 3600 * 0.5;
            var report = reopened.Crew.TakeOfflineReport();
            Assert.NotNull(report);
            Assert.InRange(reopened.Session.Save.Coins - coins, (long)(rate * effective) - 1, (long)(rate * effective) + 1);
            Assert.Equal(reopened.Session.Save.Coins - coins, report.CoinsGained);
            Assert.Equal((long)(hours * 3600000), report.AwayMs);
            Assert.Equal(hours > 10.0, report.Capped);
            Assert.Equal((long)(Math.Min(hours, 2.0) * 3600000), report.FullRateMs);
            Assert.Null(reopened.Crew.TakeOfflineReport());
        }
    }

    [Fact]
    public void A_long_gap_while_open_counts_as_offline_and_leaves_a_summary()
    {
        var (game, clock, _) = TestSupport.NewGame();
        var save = game.Session.Save;
        save.Crew.Units[Id(game.Session.Config, 2)] = 40;
        var rate = Rate(game);
        var coins = save.Coins;

        clock.AdvanceSeconds(3 * 3600); // the PC slept
        game.Crew.Sync();

        var expected = rate * (2 * 3600 + 1 * 3600 * 0.5);
        Assert.InRange(save.Coins - coins, (long)expected - 1, (long)expected + 1);
        Assert.NotNull(game.Crew.TakeOfflineReport());
    }

    [Fact]
    public void Nobody_hired_means_no_crew_summary()
    {
        var (game, clock, dir) = TestSupport.NewGame();
        game.Crew.MarkSeen();
        clock.AdvanceSeconds(5 * 3600);
        var reopened = TestSupport.NewGame(saveDir: dir, clock: clock).Game;
        Assert.Null(reopened.Crew.TakeOfflineReport());
        Assert.Equal(0, reopened.Session.Save.Coins);
    }

    [Fact]
    public void Turning_the_clock_back_credits_nothing_and_never_takes_coins()
    {
        var (game, clock, dir) = TestSupport.NewGame();
        var save = game.Session.Save;
        save.Crew.Units[Id(game.Session.Config, 3)] = 50;
        var rate = Rate(game);
        game.Crew.MarkSeen();
        var coins = save.Coins;

        clock.AdvanceSeconds(-6 * 3600);
        game.Crew.Sync();
        Assert.Equal(coins, save.Coins);

        // Reopened with the clock still back: nothing owed, nothing lost.
        var reopened = TestSupport.NewGame(saveDir: dir, clock: clock).Game;
        Assert.Equal(coins, reopened.Session.Save.Coins);
        Assert.Null(reopened.Crew.TakeOfflineReport());

        // Back to the real time and one minute more: only that minute is paid, not the six hours twice.
        clock.AdvanceSeconds(6 * 3600 + 60);
        reopened.Crew.Sync();
        Assert.InRange(reopened.Session.Save.Coins - coins, (long)(rate * 60) - 1, (long)(rate * 60) + 1);
    }

    [Fact]
    public void A_crew_cursor_in_the_future_is_not_paid()
    {
        var (game, clock, dir) = TestSupport.NewGame();
        var save = game.Session.Save;
        save.Crew.Units[Id(game.Session.Config, 3)] = 50;
        save.Crew.LastCreditedAtMs = clock.UtcNowMs + 50L * 3600 * 1000; // an edited save
        game.Session.Persist();
        var coins = save.Coins;

        var reopened = TestSupport.NewGame(saveDir: dir, clock: clock).Game;
        Assert.Equal(coins, reopened.Session.Save.Coins);
        Assert.Equal(clock.UtcNowMs, reopened.Session.Save.Crew.LastCreditedAtMs);
    }

    // ------------------------------------------------------------------ XP and fish

    [Fact]
    public void The_crews_xp_goes_into_the_fisher_total_and_climbs_levels()
    {
        var (game, clock, dir) = TestSupport.NewGame();
        var config = game.Session.Config;
        var save = game.Session.Save;
        save.Crew.Units[Id(config, 9)] = 100;
        game.Crew.MarkSeen();
        var xpRate = CrewRules.XpPerSecond(config, save.Crew);
        Assert.True(xpRate > 0);

        clock.AdvanceSeconds(10 * 3600);
        var reopened = TestSupport.NewGame(saveDir: dir, clock: clock).Game;
        var after = reopened.Session.Save;
        var expected = xpRate * (2 * 3600 + 8 * 3600 * 0.5);

        Assert.InRange(after.FisherXpTotal, (long)expected - 1, (long)expected + 1);
        Assert.Equal(after.FisherXpTotal, after.Crew.XpEarned);
        Assert.True(after.FisherLevel > 1);
        var report = reopened.Crew.TakeOfflineReport();
        Assert.Equal(after.FisherXpTotal, report.XpGained);
        Assert.Contains(after.FisherLevel, report.LevelsReached);

        // The total XP rebuilds the level on the curve (TD-038): the Crew's XP counts like fishing XP.
        var (level, xp) = FishingIdle.GameService.Fishing.FisherLevelRules.LevelForTotalXp(reopened.Session.Config, after.FisherXpTotal);
        Assert.Equal(level, after.FisherLevel);
        Assert.Equal(xp, after.FisherXp);
    }

    [Fact]
    public void Milestones_do_not_multiply_xp()
    {
        var config = TestSupport.RealConfig();
        var crew = new CrewState();
        crew.Units[Id(config, 0)] = 400;
        Assert.Equal(Member(config, 0).XpPerSecond * 400, CrewRules.XpPerSecond(config, crew), 9);
    }

    [Fact]
    public void The_crew_never_fishes()
    {
        var (game, clock, dir) = TestSupport.NewGame();
        var save = game.Session.Save;
        foreach (var m in game.Session.Config.Crew.Members)
        {
            save.Crew.Units[m.Id] = 120;
        }

        for (var i = 0; i < 300; i++)
        {
            clock.AdvanceSeconds(1);
            game.Crew.Sync();
        }

        game.Crew.MarkSeen();
        clock.AdvanceSeconds(8 * 3600);
        var reopened = TestSupport.NewGame(saveDir: dir, clock: clock).Game;
        var after = reopened.Session.Save;

        Assert.True(after.Coins > 0);
        Assert.Empty(after.FishingBox);
        Assert.Empty(after.Aquarium);
        Assert.Empty(after.SpeciesRecords);
        Assert.Equal(0, after.Stats.TotalCatches);
        Assert.Null(reopened.Fishing.TakeOfflineReport());
    }

    // ------------------------------------------------------------------ save

    [Fact]
    public void Hired_crew_survives_closing_the_game()
    {
        var (game, clock, dir) = TestSupport.NewGame();
        var config = game.Session.Config;
        game.Session.Save.Coins = 10_000;
        game.Crew.Hire(Id(config, 0), CrewBuyMode.Ten);

        var reopened = TestSupport.NewGame(saveDir: dir, clock: clock).Game;
        Assert.Equal(10, reopened.Session.Save.Crew.UnitsOf(Id(config, 0)));
        Assert.Contains("\"crew\"", File.ReadAllText(Path.Combine(dir, JsonFilePlayerRepository.SaveFileName)));
    }

    [Fact]
    public void A_version_12_save_starts_the_crew_empty_and_owes_nothing_for_the_past()
    {
        var (game, clock, dir) = TestSupport.NewGame();
        game.Session.Save.Coins = 5_000;
        game.Session.Persist();

        // The same player as a version 12 file, from before the Crew.
        var path = Path.Combine(dir, JsonFilePlayerRepository.SaveFileName);
        var json = JObject.Parse(File.ReadAllText(path));
        json["save_version"] = 12;
        json.Remove("crew");
        File.WriteAllText(path, json.ToString());

        clock.AdvanceSeconds(20 * 3600);
        var reopened = TestSupport.NewGame(saveDir: dir, clock: clock).Game;
        var save = reopened.Session.Save;

        Assert.True(PlayerSave.CurrentVersion >= 13);
        Assert.Equal(PlayerSave.CurrentVersion, save.SaveVersion);
        Assert.NotNull(save.Crew);
        Assert.Empty(save.Crew.Units);
        Assert.Equal(clock.UtcNowMs, save.Crew.LastCreditedAtMs);
        Assert.Equal(5_000, save.Coins);
        Assert.Null(reopened.Crew.TakeOfflineReport());
        Assert.Contains("\"save_version\": " + PlayerSave.CurrentVersion, File.ReadAllText(path));
    }

    [Fact]
    public void A_save_with_negative_crew_units_is_not_trusted()
    {
        var save = new PlayerSave { PlayerId = "x", CurrentMapId = "map_01" };
        save.Crew.Units["crew_01"] = -5;
        Assert.Contains("crew has malformed units", SaveValidator.Validate(save));
    }

    // ------------------------------------------------------------------ test tools and format

    [Fact]
    public void Moving_time_forward_as_online_pays_the_crew_in_full()
    {
        var (game, _, _) = TestSupport.NewGame();
        game.Session.DevToolsEnabled = true;
        var save = game.Session.Save;
        save.Crew.Units[Id(game.Session.Config, 3)] = 50;
        var rate = Rate(game);
        var coins = save.Coins;

        Assert.True(game.DevTools.AdvanceTime(5, online: true).Succeeded);
        game.Crew.Sync();

        Assert.InRange(save.Coins - coins, (long)(rate * 5 * 3600) - 1, (long)(rate * 5 * 3600) + 1);
        Assert.Null(game.Crew.TakeOfflineReport());
    }

    [Theory]
    [InlineData(0.0, "0/s")]
    [InlineData(0.006, "0,006/s")]
    [InlineData(0.25, "0,25/s")]
    [InlineData(12.56, "12,5/s")]
    [InlineData(40.0, "40/s")]
    [InlineData(845_320.7, "845.320/s")]
    [InlineData(3_450_000.0, "3,4 mi/s")]
    [InlineData(1.1e9, "1,1 bi/s")]
    public void Income_per_second_reads_in_brazilian_portuguese(double value, string expected) => Assert.Equal(expected, Format.PerSecond(value));
}
