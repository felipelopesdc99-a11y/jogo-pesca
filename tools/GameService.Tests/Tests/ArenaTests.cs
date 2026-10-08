using FishingIdle.GameService.Aquarium;
using FishingIdle.GameService.Arena;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using Xunit;

namespace FishingIdle.GameService.Tests;

public sealed class ArenaTests
{
    private static (LocalGame Game, ManualClock Clock, string Dir) Fighter(int fish = 6)
    {
        var (game, clock, dir) = TestSupport.NewGame();
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 1800, stepSeconds: 30);
        game.Fishing.StopFishing();
        game.Aquarium.KeepCatches(game.Fishing.GetFishingBox().Take(fish).Select(c => c.CatchId).ToList());
        var ids = game.Aquarium.GetAquarium(AquariumSort.Size).Fish.Select(f => f.FishId).ToList();
        for (var i = 0; i < fish; i++)
        {
            game.Cardume.SetSlot(i + 1, ids[i]);
        }

        return (game, clock, dir);
    }

    private static Fighter F(double hp, double atk, double def, double spd) =>
        new Fighter { SpeciesId = "x", Stats = new FishStats { Hp = hp, Attack = atk, Defense = def, Speed = spd } };

    [Fact]
    public void A_battle_is_deterministic_for_the_same_seed()
    {
        var config = TestSupport.RealConfig();
        var a = new List<Fighter> { F(100, 15, 8, 100), F(90, 18, 5, 102), null, null, null, null };
        var b = new List<Fighter> { F(120, 12, 12, 95), null, null, null, null, null };

        var x = BattleEngine.Resolve(config, a, b, new Rng(5));
        var y = BattleEngine.Resolve(config, a, b, new Rng(5));

        Assert.Equal(x.Winner, y.Winner);
        Assert.Equal(x.Events.Select(e => (e.Time, e.Damage)), y.Events.Select(e => (e.Time, e.Damage)));
        Assert.Equal(x.Events.Select(e => e.Time).OrderBy(t => t), x.Events.Select(e => e.Time));
    }

    [Fact]
    public void Targets_follow_the_fixed_order_and_defeated_slots_stay_empty()
    {
        var config = TestSupport.RealConfig();
        var a = new List<Fighter> { F(500, 40, 10, 100), null, null, null, null, null };
        var b = new List<Fighter> { null, F(50, 5, 5, 100), F(50, 5, 5, 100), null, F(50, 5, 5, 100), null };

        var outcome = BattleEngine.Resolve(config, a, b, new Rng(1));
        var targets = outcome.Events.Where(e => e.AttackerSide == 0).Select(e => e.TargetPosition).ToList();

        Assert.Equal(0, outcome.Winner);
        Assert.Equal(targets.OrderBy(t => t), targets); // 2, then 3, then 5: never back to a defeated slot
        Assert.Equal(new[] { 2, 3, 5 }, targets.Distinct());
    }

    [Fact]
    public void Damage_has_a_floor_so_defense_never_makes_a_fish_immune()
    {
        var config = TestSupport.RealConfig();
        var outcome = BattleEngine.Resolve(config,
            new List<Fighter> { F(100, 10, 0, 100), null, null, null, null, null },
            new List<Fighter> { F(100, 1, 100000, 100), null, null, null, null, null }, new Rng(2));

        Assert.All(outcome.Events.Where(e => e.AttackerSide == 0), e => Assert.True(e.Damage >= 10 * 0.1 - 1e-9));
        Assert.Equal(0, outcome.Winner);
    }

    [Fact]
    public void Micro_rng_only_perturbs_damage_by_three_percent()
    {
        var config = TestSupport.RealConfig();
        var outcome = BattleEngine.Resolve(config,
            new List<Fighter> { F(10000, 100, 0, 100), null, null, null, null, null },
            new List<Fighter> { F(10000, 100, 0, 100), null, null, null, null, null }, new Rng(3));

        Assert.All(outcome.Events, e => Assert.InRange(e.Damage, 97 - 1e-9, 103 + 1e-9));
    }

    [Fact]
    public void A_new_player_starts_last_with_full_energy_and_three_opponents_just_above()
    {
        var (game, _, _) = TestSupport.NewGame();

        var arena = game.Arena.GetArena();

        Assert.Equal(201, arena.Rank);
        Assert.Equal(24, arena.Energy);
        Assert.Equal(3, arena.Opponents.Count);
        Assert.Equal(3, arena.Opponents.Select(o => o.ParticipantId).Distinct().Count());
        Assert.All(arena.Opponents, o => Assert.InRange(o.Rank, 201 - 21, 200)); // ~10% window above
        Assert.Equal(ServiceError.CardumeEmpty, arena.AttackBlocker);
    }

    [Fact]
    public void Opponents_survive_a_restart_and_the_single_reroll_is_then_locked()
    {
        var (game, clock, dir) = TestSupport.NewGame();
        var first = game.Arena.GetArena().Opponents.Select(o => o.ParticipantId).ToList();

        var (reopened, _, _) = TestSupport.NewGame(saveDir: dir, clock: clock);
        Assert.Equal(first, reopened.Arena.GetArena().Opponents.Select(o => o.ParticipantId).ToList());

        Assert.True(reopened.Arena.Reroll().Succeeded);
        Assert.Equal(ServiceError.NoRerollsLeft, reopened.Arena.Reroll().Error);
        var (again, _, _) = TestSupport.NewGame(saveDir: dir, clock: clock);
        Assert.Equal(ServiceError.NoRerollsLeft, again.Arena.Reroll().Error);
    }

    [Fact]
    public void Attacking_spends_energy_swaps_on_victory_and_draws_a_new_set()
    {
        var (game, _, _) = Fighter();
        var before = game.Arena.GetArena();

        var report = game.Arena.Attack(0);

        Assert.True(report.Succeeded, report.ErrorMessage);
        var after = game.Arena.GetArena();
        Assert.Equal(23, after.Energy);
        if (report.Value.PlayerWon)
        {
            Assert.Equal(before.Opponents[0].Rank, after.Rank);
            Assert.Equal(12, after.Honor);
        }
        else
        {
            Assert.Equal(before.Rank, after.Rank);
            Assert.Equal(0, after.Honor); // Honor never below 0
        }

        Assert.Single(after.History);
        Assert.Equal(1, after.RerollsLeft);
        Assert.NotEmpty(report.Value.Outcome.Events);
    }

    [Fact]
    public void Energy_runs_out_and_comes_back_one_point_per_hour()
    {
        var (game, clock, _) = Fighter();
        for (var i = 0; i < 24; i++)
        {
            Assert.True(game.Arena.Attack(0).Succeeded);
        }

        Assert.Equal(ServiceError.NotEnoughEnergy, game.Arena.Attack(0).Error);
        clock.AdvanceSeconds(3600);
        Assert.Equal(1, game.Arena.GetArena().Energy);
        clock.AdvanceSeconds(100 * 3600);
        Assert.Equal(24, game.Arena.GetArena().Energy);
    }

    [Fact]
    public void The_arena_is_locked_while_the_cardume_is_on_an_expedition()
    {
        var (game, _, _) = Fighter();
        game.Expeditions.Start("exp_30m");

        Assert.Equal(ServiceError.CardumeLocked, game.Arena.Attack(0).Error);
    }

    [Fact]
    public void Other_players_attack_you_while_you_are_away()
    {
        var (game, clock, _) = Fighter();
        // Attackers come from below; the last place has nobody below, so start mid-table.
        var ranking = game.Session.Save.Arena.Ranking;
        ranking.Remove("player");
        ranking.Insert(99, "player");
        var before = game.Arena.GetArena();
        Assert.Equal(100, before.Rank);

        clock.AdvanceSeconds(24 * 3600);
        var defenses = game.Arena.Update();
        var after = game.Arena.GetArena();

        Assert.NotEmpty(defenses);
        Assert.All(defenses, d => Assert.True(d.IsDefense));
        // OD-024: won defenses give Honor, lost ones take a little (never below the minimum).
        Assert.Equal(defenses.Sum(d => d.HonorChange), after.Honor - before.Honor);
        Assert.All(defenses.Where(d => !d.PlayerWon), d => Assert.InRange(d.HonorChange, -2, 0));
        Assert.Empty(game.Arena.Update()); // the same hours are never checked twice
    }

    [Fact]
    public void Near_the_top_the_window_falls_back_to_the_nearest_valid_ranks()
    {
        var (game, _, _) = TestSupport.NewGame();
        var ranking = game.Session.Save.Arena.Ranking;
        ranking.Remove("player");
        ranking.Insert(0, "player");
        game.Session.Save.Arena.Opponents.Clear();

        var arena = game.Arena.GetArena();

        Assert.Equal(1, arena.Rank);
        Assert.Equal(new[] { 4, 3, 2 }, arena.Opponents.Select(o => o.Rank)); // easiest first (A-133)
    }

    [Fact]
    public void Opponents_go_from_the_easiest_to_the_strongest()
    {
        var (game, _, _) = TestSupport.NewGame();

        var ranks = game.Arena.GetArena().Opponents.Select(o => o.Rank).ToList();

        Assert.Equal(ranks.OrderByDescending(r => r).ToList(), ranks);
    }
}
