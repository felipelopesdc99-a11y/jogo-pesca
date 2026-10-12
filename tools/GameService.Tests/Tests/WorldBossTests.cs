using FishingIdle.GameService.Aquarium;
using FishingIdle.GameService.Arena;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Persistence;
using FishingIdle.GameService.WorldBoss;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>Boss Mundial (M24-T29, A-165): the 19:00–19:10 Brasília window, automatic attacks, bands and the single payment.</summary>
public sealed class WorldBossTests
{
    private const long HourMs = 3_600_000L;

    /// <summary>A player with a full Cardume of 6 fish, the clock still before 19:00 of the next Brasília day.</summary>
    private static (LocalGame Game, ManualClock Clock, string Dir, long Day) Player(GameConfig config = null, int fish = 6)
    {
        var (game, clock, dir) = TestSupport.NewGame(config);
        if (fish > 0)
        {
            game.Fishing.StartFishing();
            TestSupport.PlayFor(game, clock, 1800, stepSeconds: 30);
            game.Fishing.StopFishing();
            game.Aquarium.KeepCatches(game.Fishing.GetFishingBox().Take(fish).Select(c => c.CatchId).ToList());
            var ids = game.Aquarium.GetAquarium(AquariumSort.Size).Fish.Select(f => f.FishId).ToList();
            for (var i = 0; i < fish; i++)
            {
                game.Cardume.SetSlot(i + 1, ids[i]);
            }
        }

        var boss = game.Session.Config.WorldBoss;
        var day = WorldBossRules.DayOf(boss, clock.UtcNowMs) + 1;
        clock.UtcNowMs = WorldBossRules.WindowStartMs(boss, day) - HourMs;
        return (game, clock, dir, day);
    }

    private static long At(LocalGame game, long day, int hour, int minute, int second = 0)
    {
        // Brasília (UTC−3): local midnight of the day is 03:00 UTC.
        var offset = game.Session.Config.WorldBoss.Schedule.UtcOffsetMinutes * 60_000L;
        return day * WorldBossRules.DayMs - offset + ((hour * 60L + minute) * 60L + second) * 1000L;
    }

    /// <summary>Updates every <paramref name="step"/> seconds for <paramref name="seconds"/>, like the running game.</summary>
    private static List<WorldBossUpdate> Run(LocalGame game, ManualClock clock, double seconds, double step = 1)
    {
        var updates = new List<WorldBossUpdate>();
        for (var t = 0.0; t < seconds - 1e-9; t += step)
        {
            clock.AdvanceSeconds(step);
            updates.Add(game.WorldBoss.Update());
        }

        return updates;
    }

    [Fact]
    public void The_window_is_19_00_to_19_10_in_Brasilia_and_the_day_turns_at_Brasilia_midnight()
    {
        var boss = TestSupport.RealConfig().WorldBoss;
        long Utc(int month, int day, int hour, int minute, int second = 0) => new DateTimeOffset(2026, month, day, hour, minute, second, TimeSpan.Zero).ToUnixTimeMilliseconds();

        // 12/10/2026 19:00 in Brasília is 22:00 UTC.
        Assert.False(WorldBossRules.IsOpen(boss, Utc(10, 12, 21, 59, 59)));
        Assert.True(WorldBossRules.IsOpen(boss, Utc(10, 12, 22, 0)));
        Assert.True(WorldBossRules.IsOpen(boss, Utc(10, 12, 22, 9, 59)));
        Assert.False(WorldBossRules.IsOpen(boss, Utc(10, 12, 22, 10)));

        // 23:30 in Brasília is already 13/10 in UTC, but still the 12th's event day; the next one is 13/10 at 19:00.
        var late = Utc(10, 13, 2, 30);
        Assert.Equal(new DateTime(2026, 10, 12), WorldBossRules.DateOf(WorldBossRules.DayOf(boss, late)));
        Assert.Equal(Utc(10, 13, 22, 0), WorldBossRules.NextStartMs(boss, late));
        Assert.Equal(Utc(10, 12, 22, 0), WorldBossRules.NextStartMs(boss, Utc(10, 12, 12, 0)));
        Assert.Equal(new DateTime(2026, 10, 13), WorldBossRules.DateOf(WorldBossRules.DayOf(boss, Utc(10, 13, 3, 0))));
        Assert.Equal(200, WorldBossRules.AttacksPerWindow(boss));
    }

    [Fact]
    public void Nothing_happens_outside_the_window()
    {
        var (game, clock, _, day) = Player();
        clock.UtcNowMs = At(game, day, 18, 59, 30);
        Assert.All(Run(game, clock, 25), u => Assert.False(u.Started || u.AttacksCredited > 0));
        var view = game.WorldBoss.GetWorldBoss();
        Assert.False(view.Open);
        Assert.Equal(5, view.SecondsToStart, 3);

        // Opened only after 19:10: the day is lost, nothing is paid.
        clock.UtcNowMs = At(game, day, 19, 10, 5);
        var coins = game.Session.Save.Coins;
        Assert.All(Run(game, clock, 30), u => Assert.True(!u.Started && u.AttacksCredited == 0 && u.Paid == null));
        Assert.Equal(0, game.Session.Save.WorldBoss.Day);
        Assert.Equal(coins, game.Session.Save.Coins);
    }

    [Fact]
    public void The_Cardume_attacks_once_per_interval_while_the_game_is_open()
    {
        var (game, clock, _, day) = Player();
        clock.UtcNowMs = At(game, day, 19, 0, 0);
        var first = game.WorldBoss.Update();
        Assert.True(first.Started);
        Assert.Equal(0, first.AttacksCredited);

        var updates = Run(game, clock, 30, step: 0.5);
        Assert.Equal(10, updates.Sum(u => u.AttacksCredited));
        var view = game.WorldBoss.GetWorldBoss();
        Assert.True(view.Open);
        Assert.Equal(10, view.Attacks);
        Assert.True(view.Damage > 0);
        Assert.True(view.LastAttackDamage > 0);
        Assert.Equal(570, view.SecondsLeft, 3);
        Assert.Equal(ServiceError.None, view.AttackBlocker);
    }

    [Fact]
    public void An_attack_uses_the_Arena_hit_rule_with_a_small_roll()
    {
        var config = TestSupport.RealConfig();
        var fish = new FishStats { Hp = 500, Attack = 100, Defense = 50, Speed = 100 };
        var team = new List<FishStats> { fish, null, null, null, null, null };

        // 3 s ÷ the Arena's 2 s at speed 100 = 1,5 hits per attack; Attack 100 × roll 0,97–1,03, no boss defense.
        for (var i = 0; i < 50; i++)
        {
            var bySlot = new long[6];
            var damage = WorldBossRules.Attack(config, team, Rng.For(7, i, 0), bySlot);
            Assert.InRange(damage, 145, 155);
            Assert.Equal(damage, bySlot[0]);
            Assert.Equal(0, bySlot[1]);
        }

        // The same draw as one Arena hit.
        var roll = BattleEngine.RollDamage(config.Arena.Combat, Rng.For(7, 3, 0));
        Assert.Equal((long)Math.Round(BattleEngine.HitDamage(config.Arena.Combat, 100, 0, roll) * 1.5), WorldBossRules.Attack(config, team, Rng.For(7, 3, 0)));

        // The whole window at the average roll: 150 × 200.
        Assert.Equal(30_000, WorldBossRules.FullWindowDamage(config, team), 6);

        // A faster fish hits more often; no fish, no damage.
        var fast = new List<FishStats> { new FishStats { Attack = 100, Speed = 200 } };
        Assert.Equal(60_000, WorldBossRules.FullWindowDamage(config, fast), 6);
        Assert.Equal(0, WorldBossRules.Attack(config, new List<FishStats> { null, null }, new Rng(1)));
    }

    [Fact]
    public void An_empty_Cardume_does_not_attack_and_says_why()
    {
        var (game, clock, _, day) = Player(fish: 0);
        clock.UtcNowMs = At(game, day, 19, 0, 0);
        Assert.True(game.WorldBoss.Update().Started);
        Assert.Equal(0, Run(game, clock, 60).Sum(u => u.AttacksCredited));
        var view = game.WorldBoss.GetWorldBoss();
        Assert.Equal(ServiceError.CardumeEmpty, view.AttackBlocker);
        Assert.Equal(0, view.Damage);
        Assert.Empty(view.Ranking);

        clock.UtcNowMs = At(game, day, 19, 10, 1);
        var coins = game.Session.Save.Coins;
        Assert.Null(game.WorldBoss.Update().Paid);
        Assert.Equal(coins, game.Session.Save.Coins);
        Assert.True(game.Session.Save.WorldBoss.Paid);
    }

    [Fact]
    public void Ten_minutes_reach_the_last_band_and_pay_once_at_19_10()
    {
        var (game, clock, dir, day) = Player();
        var boss = game.Session.Config.WorldBoss;
        clock.UtcNowMs = At(game, day, 19, 0, 0);
        game.WorldBoss.Update();
        var updates = Run(game, clock, 600, step: 1);
        var reached = updates.Where(u => u.BandReached > 0).Select(u => u.BandReached).ToList();
        Assert.Equal(Enumerable.Range(1, boss.Bands.Count), reached);
        Assert.Equal(200, game.Session.Save.WorldBoss.Attacks);

        var paid = updates.Select(u => u.Paid).Single(p => p != null);
        var level = game.Session.Save.FisherLevel;
        var top = boss.Bands.Last();
        Assert.Equal(boss.Bands.Count, paid.Band);
        Assert.Equal(WorldBossRules.Coins(boss, top, level), paid.Coins);
        Assert.Equal(WorldBossRules.IncomeAt(boss, level).CoinsPerHour * top.IncomeMinutes / 60.0, paid.Coins, 0);
        Assert.Equal(top.ClothingFragments, paid.ClothingFragments);
        Assert.Equal(top.BoatFragments, paid.BoatFragments);
        Assert.Equal(1, paid.Rank);
        Assert.Equal(0, paid.Dollars);
        Assert.Equal(new DateTime(1970, 1, 1).AddDays(day), paid.Date);

        // Never twice: more updates, and reopening the game, pay nothing more.
        var coins = game.Session.Save.Coins;
        var shells = game.Session.Save.Shells;
        Assert.All(Run(game, clock, 120, step: 5), u => Assert.Null(u.Paid));
        var reopened = TestSupport.NewGame(game.Session.Config, dir, clock).Game;
        Assert.Null(reopened.WorldBoss.Update().Paid);
        Assert.Equal(coins, reopened.Session.Save.Coins);
        Assert.Equal(shells, reopened.Session.Save.Shells);

        var view = reopened.WorldBoss.GetWorldBoss();
        Assert.False(view.Open);
        Assert.NotNull(view.LastResult);
        Assert.Equal(paid.Coins, view.LastResult.Coins);
        Assert.Equal(0, view.Damage);
    }

    [Fact]
    public void Two_minutes_reach_the_band_of_twenty_percent()
    {
        var (game, clock, _, day) = Player();
        var boss = game.Session.Config.WorldBoss;
        clock.UtcNowMs = At(game, day, 19, 0, 0);
        game.WorldBoss.Update();
        Run(game, clock, 120, step: 3);
        var state = game.Session.Save.WorldBoss;
        Assert.Equal(40, state.Attacks);
        var expected = boss.Bands.Count(b => b.DamagePercent <= 100.0 * state.Damage / state.ReferenceDamage);
        Assert.Equal(expected, state.Band);
        Assert.Equal(2, state.Band);

        var view = game.WorldBoss.GetWorldBoss();
        Assert.Equal(3, view.NextBand);
        Assert.Equal(WorldBossRules.Threshold(boss, 3, state.ReferenceDamage), view.Bands[2].ThresholdDamage);
        Assert.True(view.Bands[1].Reached);
        Assert.False(view.Bands[2].Reached);
    }

    [Fact]
    public void Closing_the_game_stops_the_attacks_and_reopening_continues_the_day()
    {
        var (game, clock, dir, day) = Player();
        clock.UtcNowMs = At(game, day, 19, 0, 0);
        game.WorldBoss.Update();
        Run(game, clock, 60, step: 1);
        var before = game.Session.Save.WorldBoss.Damage;
        Assert.Equal(20, game.Session.Save.WorldBoss.Attacks);

        // Closed from 19:01 to 19:05: reopening counts nothing for that time and goes on from there.
        clock.UtcNowMs = At(game, day, 19, 5, 0);
        var reopened = TestSupport.NewGame(game.Session.Config, dir, clock).Game;
        var first = reopened.WorldBoss.Update();
        Assert.False(first.Started);
        Assert.Equal(0, first.AttacksCredited);
        Assert.Equal(before, reopened.Session.Save.WorldBoss.Damage);
        Run(reopened, clock, 30, step: 1);
        Assert.Equal(30, reopened.Session.Save.WorldBoss.Attacks);
        Assert.True(reopened.Session.Save.WorldBoss.Damage > before);

        // A freeze with the game open counts at most max_catch_up_attacks.
        clock.AdvanceSeconds(60);
        Assert.Equal(reopened.Session.Config.WorldBoss.Attack.MaxCatchUpAttacks, reopened.WorldBoss.Update().AttacksCredited);
    }

    [Fact]
    public void Rewards_of_a_closed_game_are_paid_at_the_next_opening()
    {
        var (game, clock, dir, day) = Player();
        clock.UtcNowMs = At(game, day, 19, 0, 0);
        game.WorldBoss.Update();
        Run(game, clock, 330, step: 3);
        Assert.False(game.Session.Save.WorldBoss.Paid);

        // Closed at 19:05:30 (55% of the day's maximum), opened the next morning.
        clock.UtcNowMs = At(game, day + 1, 9, 0, 0);
        var reopened = TestSupport.NewGame(game.Session.Config, dir, clock).Game;
        var paid = reopened.WorldBoss.Update().Paid;
        Assert.NotNull(paid);
        Assert.Equal(110, paid.Attacks);
        Assert.Equal(4, paid.Band);
        Assert.True(paid.Coins > 0);
        Assert.Null(reopened.WorldBoss.Update().Paid);

        // That evening a new day starts and the old record is replaced.
        clock.UtcNowMs = At(game, day + 1, 19, 0, 1);
        Assert.True(reopened.WorldBoss.Update().Started);
        Assert.Equal(day + 1, reopened.Session.Save.WorldBoss.Day);
        Assert.Equal(0, reopened.Session.Save.WorldBoss.Damage);
    }

    [Fact]
    public void A_clock_turned_back_gives_no_extra_attacks_or_payments()
    {
        var (game, clock, _, day) = Player();
        clock.UtcNowMs = At(game, day, 19, 0, 0);
        game.WorldBoss.Update();
        Run(game, clock, 30, step: 1);
        var attacks = game.Session.Save.WorldBoss.Attacks;

        // Turned back an hour: time is frozen at the highest moment seen (TD-030), so nothing is due.
        clock.UtcNowMs -= HourMs;
        Assert.All(Run(game, clock, 60, step: 1), u => Assert.True(u.AttacksCredited == 0 && !u.Started && u.Paid == null));
        Assert.Equal(attacks, game.Session.Save.WorldBoss.Attacks);

        // Back to real time: the event goes on and pays once.
        clock.UtcNowMs = At(game, day, 19, 0, 31);
        Run(game, clock, 600, step: 3);
        Assert.True(game.Session.Save.WorldBoss.Paid);
        var coins = game.Session.Save.Coins;
        clock.UtcNowMs -= 30 * 60_000L;
        Run(game, clock, 60, step: 3);
        Assert.Equal(coins, game.Session.Save.Coins);
    }

    [Fact]
    public void The_top_10_Dollars_pay_only_with_the_online_flag()
    {
        // Local game (flag off): the ranking shows only the player, no Dólares.
        var (game, clock, _, day) = Player();
        clock.UtcNowMs = At(game, day, 19, 0, 0);
        game.WorldBoss.Update();
        Run(game, clock, 30, step: 3);
        var view = game.WorldBoss.GetWorldBoss();
        Assert.Single(view.Ranking);
        Assert.True(view.Ranking[0].IsPlayer);
        Assert.Equal(0, view.Ranking[0].Dollars);
        Assert.False(view.OnlineRewards);
        Assert.Equal(new long[] { 50, 40, 30, 25, 20, 15, 10, 10, 5, 5 }, view.DollarsByRank);
        var dollars = game.Session.Save.Dollars;
        clock.UtcNowMs = At(game, day, 19, 10, 0);
        Assert.Equal(0, game.WorldBoss.Update().Paid.Dollars);
        Assert.Equal(dollars, game.Session.Save.Dollars);

        // With the flag on, the 1st of the day gets 50.
        var online = TestSupport.ConfigWith(GameConfigLoader.WorldBossFile, j =>
        {
            var root = JObject.Parse(j);
            root["ranking"]!["online_rewards_enabled"] = true;
            return root.ToString();
        }, certainCatch: true);
        Assert.True(online.Succeeded, string.Join("\n", online.Errors));
        var (game2, clock2, _, day2) = Player(online.Config);
        clock2.UtcNowMs = At(game2, day2, 19, 0, 0);
        game2.WorldBoss.Update();
        Run(game2, clock2, 30, step: 3);
        var before = game2.Session.Save.Dollars;
        clock2.UtcNowMs = At(game2, day2, 19, 10, 0);
        var paid = game2.WorldBoss.Update().Paid;
        Assert.Equal(50, paid.Dollars);
        Assert.Equal(before + 50, game2.Session.Save.Dollars);
    }

    [Fact]
    public void A_version_18_save_gets_an_empty_world_boss()
    {
        var (game, clock, dir) = TestSupport.NewGame();
        var path = Path.Combine(dir, JsonFilePlayerRepository.SaveFileName);
        var json = JObject.Parse(File.ReadAllText(path));
        json["save_version"] = 18;
        json.Remove("world_boss");
        File.WriteAllText(path, json.ToString());

        var reopened = TestSupport.NewGame(game.Session.Config, dir, clock).Game;
        Assert.Equal(19, PlayerSave.CurrentVersion);
        Assert.Equal(PlayerSave.CurrentVersion, reopened.Session.Save.SaveVersion);
        Assert.NotNull(reopened.Session.Save.WorldBoss);
        Assert.Equal(0, reopened.Session.Save.WorldBoss.Day);
        Assert.Contains("\"world_boss\"", File.ReadAllText(path));

        Assert.Contains("world boss malformed", SaveValidator.Validate(new PlayerSave
        {
            PlayerId = "p", CurrentMapId = "map_01", WorldBoss = new WorldBossState { Damage = -1 },
        }));
    }

    [Fact]
    public void The_config_refuses_a_window_that_crosses_midnight_and_unordered_bands()
    {
        var late = TestSupport.ConfigWith(GameConfigLoader.WorldBossFile, j =>
        {
            var root = JObject.Parse(j);
            root["schedule"]!["start_minute_of_day"] = 1435;
            return root.ToString();
        });
        Assert.Contains(late.Errors, e => e.Contains("world_boss.json") && e.Contains("schedule"));

        var bands = TestSupport.ConfigWith(GameConfigLoader.WorldBossFile, j =>
        {
            var root = JObject.Parse(j);
            root["bands"]![1]!["damage_percent"] = 1;
            return root.ToString();
        });
        Assert.Contains(bands.Errors, e => e.Contains("world_boss.json") && e.Contains("bands"));

        var boss = TestSupport.RealConfig().WorldBoss;
        Assert.Equal(1, boss.ReferenceIncome[0].Level);
        Assert.Equal(6, boss.Bands.Count);
        Assert.False(boss.Ranking.OnlineRewardsEnabled);
    }
}
