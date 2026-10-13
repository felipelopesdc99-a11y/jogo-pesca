using FishingIdle.GameService.Clans;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Persistence;
using FishingIdle.GameService.Profile;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>
/// The Clãs (M24-T49, A-174, TD-055): Fisher Nv.100+ only, created with Dólares, 10 places plus the ones the owner buys
/// (each dearer), +1% Fisher XP for members, one fish per member in the clan's Cardume (it stays in the player's Aquarium
/// and Cardume), the clan's Strength as the sum of those fish (the Cardume's own rule), and the daily ranking at 00:00
/// (Brasília) whose top 3 rewards are off in the local game and pay with the flag on.
/// </summary>
public sealed class ClanTests
{
    private static ClansConfig Clans => TestSupport.RealConfig().Clans;

    /// <summary>The config of the tests (every bite pulled out) with an edit of clans.json.</summary>
    private static GameConfig With(Action<JObject> edit)
    {
        var result = TestSupport.ConfigWith(GameConfigLoader.ClansFile, j =>
        {
            var root = JObject.Parse(j);
            edit(root);
            return root.ToString();
        }, certainCatch: true);
        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        return result.Config;
    }

    private static GameConfig Online => With(root => root["ranking"]!["online_rewards_enabled"] = true);

    /// <summary>A new game with a Pescador Nv.100 and the Dólares given.</summary>
    private static (LocalGame Game, ManualClock Clock, string Dir) Player(GameConfig config = null, long dollars = 0, int level = 100)
    {
        var (game, clock, dir) = TestSupport.NewGame(config, seed: TestSupport.FixedSeed);
        game.Session.Save.FisherLevel = level;
        game.Session.Save.Dollars = dollars;
        game.Session.DevToolsEnabled = true;
        return (game, clock, dir);
    }

    /// <summary>Keeps <paramref name="count"/> fish of a species in the Aquarium (dev tools), returns their ids.</summary>
    private static List<long> KeepFish(LocalGame game, string speciesId, int count, string size = null)
    {
        Assert.True(game.DevTools.GiveFish(speciesId, size, count).Succeeded);
        var kept = game.Aquarium.KeepCatches(game.Fishing.GetFishingBox().Select(c => c.CatchId).ToList());
        Assert.True(kept.Succeeded, kept.ErrorMessage);
        return kept.Value.Kept.Select(f => f.FishId).ToList();
    }

    /// <summary>Another player's clan, as the future server would report it.</summary>
    private static ClanRecord OtherClan(string id, string name, int members, long strengthEach, int extraSlots = 0, long createdAtMs = 1)
    {
        var clan = new ClanRecord { Id = id, Name = name, OwnerId = id + "_p0", CreatedAtMs = createdAtMs, ExtraSlots = extraSlots };
        for (var i = 0; i < members; i++)
        {
            clan.Members.Add(new ClanMemberRecord
            {
                PlayerId = id + "_p" + i, Name = "Pescador " + i, FisherLevel = 150, JoinedAtMs = i,
                FishSpeciesId = "lambari", FishLevel = 10, FishStrength = strengthEach,
            });
        }

        return clan;
    }

    // ------------------------------------------------------------------ config

    [Fact]
    public void The_config_has_the_owner_s_rules_and_provisional_numbers()
    {
        var c = Clans;
        Assert.Equal(100, c.Membership.MinFisherLevel);
        Assert.Equal(10, c.Membership.BaseMembers);
        // Owner (13/10/2026): up to 5 extra places, one per purchase, each dearer.
        Assert.Equal(15, c.Membership.MaxMembers);
        Assert.Equal(0, c.Costs.FirstCreateDollars);
        Assert.True(c.Costs.CreateDollars > 0);
        Assert.True(c.Costs.ExtraSlotGrowth > 1);
        Assert.Equal(0.01, c.XpBonus, 9);
        Assert.Equal(-180, c.Ranking.UtcOffsetMinutes);
        Assert.False(c.Ranking.OnlineRewardsEnabled);
        Assert.Equal(3, c.Ranking.RewardsByRank.Count);
        Assert.Contains(GameConfigLoader.ClansFile, GameConfigLoader.RequiredFiles);

        var bad = TestSupport.ConfigWith(GameConfigLoader.ClansFile, j =>
        {
            var root = JObject.Parse(j);
            root["membership"]!["max_members"] = 5;
            root["costs"]!["extra_slot_growth"] = 0.5;
            root["xp_bonus"] = 2.0;
            return root.ToString();
        });
        Assert.False(bad.Succeeded);
        Assert.Contains(bad.Errors, e => e.Contains("clans.json") && e.Contains("membership"));
        Assert.Contains(bad.Errors, e => e.Contains("clans.json") && e.Contains("costs"));
        Assert.Contains(bad.Errors, e => e.Contains("clans.json") && e.Contains("xp_bonus"));
    }

    // ------------------------------------------------------------------ Nv.100 and the price in Dólares

    [Fact]
    public void Below_Nv_100_nobody_creates_or_joins_a_clan()
    {
        var (game, _, _) = Player(dollars: 1_000, level: 99);
        game.Session.Save.Clans.KnownClans.Add(OtherClan("clan_x", "Outros", 3, 100));

        var view = game.Clans.GetClan();
        Assert.False(view.LevelAllows);
        Assert.Equal(ServiceError.ClanLevelTooLow, view.CreateBlocker);
        Assert.Equal(ServiceError.ClanLevelTooLow, game.Clans.Create("Os Pescadores").Error);
        Assert.Equal(ServiceError.ClanLevelTooLow, game.Clans.Join("clan_x").Error);
        Assert.Equal(1_000, game.Session.Save.Dollars);
        Assert.Null(game.Session.Save.Clans.MyClanId);

        game.Session.Save.FisherLevel = 100;
        Assert.True(game.Clans.GetClan().LevelAllows);
        Assert.True(game.Clans.Join("clan_x").Succeeded);
    }

    [Fact]
    public void The_first_clan_of_each_player_is_free_and_the_next_ones_cost_Dolares()
    {
        Assert.Equal(0, Clans.Costs.FirstCreateDollars);
        var cost = Clans.Costs.CreateDollars;
        Assert.True(cost > 0);

        // Each player's first clan is free, even with no Dólares.
        var (game, _, _) = Player(dollars: 0);
        Assert.Equal(0, game.Clans.GetClan().CreateCostDollars);
        var first = game.Clans.Create("  Os   Pescadores ");
        Assert.True(first.Succeeded, first.ErrorMessage);
        Assert.Equal(0, game.Session.Save.Dollars);
        var clan = first.Value.Clan;
        Assert.Equal("Os Pescadores", clan.Name);
        Assert.True(clan.IsOwner);
        Assert.Equal(1, clan.MemberCount);
        Assert.Equal(10, clan.Capacity);
        Assert.True(clan.Members.Single().IsPlayer);
        Assert.Equal(ServiceError.AlreadyInClan, game.Clans.Create("Outro Nome").Error);
        Assert.Equal(1, game.Session.Save.Clans.ClansCreated);

        // Deleting it (leaving the empty clan) and creating again costs create_dollars.
        Assert.True(game.Clans.Leave().Succeeded);
        Assert.Equal(cost, game.Clans.GetClan().CreateCostDollars);
        game.Session.Save.Dollars = cost - 1;
        var refused = game.Clans.Create("Os Pescadores");
        Assert.Equal(ServiceError.NotEnoughDollars, refused.Error);
        Assert.Equal("Dólares insuficientes.", refused.ErrorMessage);
        Assert.Equal(cost - 1, game.Session.Save.Dollars);
        Assert.Empty(game.Session.Save.Clans.KnownClans);

        game.Session.Save.Dollars = cost + 7;
        Assert.True(game.Clans.Create("Os Pescadores").Succeeded);
        Assert.Equal(7, game.Session.Save.Dollars);
        Assert.Equal(2, game.Session.Save.Clans.ClansCreated);

        // Per player: another player still gets the first one free.
        var (other, _, _) = Player(dollars: 0);
        Assert.Equal(0, other.Clans.GetClan().CreateCostDollars);
        Assert.True(other.Clans.Create("Outro Clã").Succeeded);
    }

    [Fact]
    public void A_save_that_already_created_a_clan_before_the_counter_pays_for_the_next()
    {
        var (game, _, _) = Player(dollars: 0);
        game.Session.Save.Clans.NextClanNumber = 2; // a v22 save that created (and deleted) one clan
        game.Session.Save.Clans.ClansCreated = 0;
        Assert.Equal(Clans.Costs.CreateDollars, game.Clans.GetClan().CreateCostDollars);
        Assert.Equal(ServiceError.NotEnoughDollars, game.Clans.Create("Os Pescadores").Error);
    }

    [Fact]
    public void The_name_is_checked_for_size_characters_and_repeats()
    {
        var (game, _, _) = Player(dollars: 1_000);
        game.Session.Save.Clans.KnownClans.Add(OtherClan("clan_x", "Lago Dourado", 1, 10));

        Assert.Equal(ServiceError.InvalidClanName, game.Clans.Create("ab").Error);
        Assert.Equal(ServiceError.InvalidClanName, game.Clans.Create(new string('a', Clans.Name.MaxLength + 1)).Error);
        Assert.Equal(ServiceError.InvalidClanName, game.Clans.Create("<clã>").Error);
        Assert.Equal(ServiceError.ClanNameTaken, game.Clans.Create("lago   DOURADO").Error);
        Assert.Equal(1_000, game.Session.Save.Dollars);
        Assert.True(game.Clans.Create("Maré-Alta_2.0").Succeeded);
    }

    // ------------------------------------------------------------------ places

    [Fact]
    public void A_clan_holds_10_members_until_the_owner_buys_places()
    {
        var (game, _, _) = Player();
        var save = game.Session.Save;
        save.Clans.KnownClans.Add(OtherClan("clan_full", "Cheio", 10, 50));
        save.Clans.KnownClans.Add(OtherClan("clan_nine", "Quase", 9, 50));
        save.Clans.KnownClans.Add(OtherClan("clan_more", "Maior", 10, 50, extraSlots: 1));

        Assert.Equal(ServiceError.ClanFull, game.Clans.Join("clan_full").Error);
        Assert.Equal(ServiceError.ClanNotFound, game.Clans.Join("clan_none").Error);

        var joined = game.Clans.Join("clan_nine");
        Assert.True(joined.Succeeded, joined.ErrorMessage);
        Assert.Equal(10, joined.Value.Clan.MemberCount);
        Assert.Equal(10, joined.Value.Clan.Capacity);
        Assert.False(joined.Value.Clan.IsOwner);
        Assert.Equal(ServiceError.AlreadyInClan, game.Clans.Join("clan_more").Error);

        // A member who is not the owner does not buy places.
        save.Dollars = 1_000;
        Assert.Equal(ServiceError.NotClanOwner, game.Clans.BuySlot().Error);
        Assert.Equal(ServiceError.NotClanOwner, game.Clans.GetClan().Clan.BuySlotBlocker);

        Assert.True(game.Clans.Leave().Succeeded);
        var bigger = game.Clans.Join("clan_more");
        Assert.True(bigger.Succeeded, bigger.ErrorMessage);
        Assert.Equal(11, bigger.Value.Clan.MemberCount);
        Assert.Equal(11, bigger.Value.Clan.Capacity);
    }

    [Fact]
    public void Each_extra_place_costs_more_Dolares_and_stops_at_the_maximum()
    {
        var config = With(root => root["membership"]!["max_members"] = 13);
        var c = config.Clans.Costs;
        var (game, _, _) = Player(config, dollars: c.CreateDollars);
        Assert.True(game.Clans.Create("Os Pescadores").Succeeded);

        var prices = new List<long>();
        for (var i = 0; i < 3; i++)
        {
            var price = game.Clans.GetClan().Clan.NextSlotPriceDollars;
            prices.Add(price);
            game.Session.Save.Dollars = price - 1;
            Assert.Equal(ServiceError.NotEnoughDollars, game.Clans.BuySlot().Error);
            game.Session.Save.Dollars = price;
            var bought = game.Clans.BuySlot();
            Assert.True(bought.Succeeded, bought.ErrorMessage);
            Assert.Equal(0, game.Session.Save.Dollars);
            Assert.Equal(11 + i, bought.Value.Clan.Capacity);
        }

        Assert.Equal(c.ExtraSlotFirstDollars, prices[0]);
        Assert.True(prices[1] > prices[0] && prices[2] > prices[1]);
        Assert.Equal((long)Math.Round(c.ExtraSlotFirstDollars * c.ExtraSlotGrowth * c.ExtraSlotGrowth, MidpointRounding.AwayFromZero), prices[2]);

        // At max_members (13 here) there is nothing more to buy.
        game.Session.Save.Dollars = 1_000_000;
        Assert.Equal(ServiceError.ClanSlotsMaxed, game.Clans.BuySlot().Error);
        Assert.Equal(0, game.Clans.GetClan().Clan.NextSlotPriceDollars);
        Assert.Equal(1_000_000, game.Session.Save.Dollars);
    }

    [Fact]
    public void Leaving_closes_an_empty_clan_and_an_owner_passes_it_on()
    {
        var cost = Clans.Costs.CreateDollars;
        var (game, _, _) = Player(dollars: cost * 2);
        var save = game.Session.Save;
        Assert.True(game.Clans.Create("Os Pescadores").Succeeded);
        Assert.True(game.Clans.Leave().Succeeded);
        Assert.Empty(save.Clans.KnownClans);
        Assert.Null(save.Clans.MyClanId);
        Assert.Equal(ServiceError.NotInClan, game.Clans.Leave().Error);

        // The name is free again; with other members, the one who joined first becomes the owner.
        Assert.True(game.Clans.Create("Os Pescadores").Succeeded);
        var clan = save.Clans.KnownClans.Single();
        clan.Members.Add(new ClanMemberRecord { PlayerId = "b", Name = "B", FisherLevel = 120, JoinedAtMs = clan.CreatedAtMs + 2 });
        clan.Members.Add(new ClanMemberRecord { PlayerId = "a", Name = "A", FisherLevel = 120, JoinedAtMs = clan.CreatedAtMs + 1 });
        Assert.True(game.Clans.Leave().Succeeded);
        Assert.Equal("a", clan.OwnerId);
        Assert.Equal(2, clan.Members.Count);
        Assert.Contains(clan, save.Clans.KnownClans);
    }

    // ------------------------------------------------------------------ the clan's Cardume and its Strength

    [Fact]
    public void The_fish_in_the_clan_s_Cardume_stays_in_the_Aquarium_and_the_player_s_Cardume()
    {
        var (game, _, _) = Player(dollars: Clans.Costs.CreateDollars);
        var fish = KeepFish(game, "lambari", 2, "large");
        Assert.True(game.Cardume.SetSlot(1, fish[0]).Succeeded);
        Assert.Equal(ServiceError.NotInClan, game.Clans.SetFish(fish[0]).Error);
        Assert.True(game.Clans.Create("Os Pescadores").Succeeded);
        Assert.Equal(ServiceError.FishNotFound, game.Clans.SetFish(999_999).Error);

        var set = game.Clans.SetFish(fish[0]);
        Assert.True(set.Succeeded, set.ErrorMessage);
        var save = game.Session.Save;
        Assert.Contains(save.Aquarium, f => f.Id == fish[0]);
        Assert.Equal(fish[0], save.CardumeSlots[0]);
        var mine = set.Value.Clan.MyFish;
        Assert.Equal(fish[0], mine.FishId);
        Assert.Equal(1, mine.CardumePosition);

        // The Strength is the Cardume's rule on that fish alone, and the clan's is the sum of its members' fish.
        var config = game.Session.Config;
        var instance = save.Aquarium.Single(f => f.Id == fish[0]);
        config.TryGetSpecies(instance.SpeciesId, out var species);
        var stats = FishRules.Stats(config, species, instance.SizeMm, instance.Level, instance.SizeCategoryId);
        var expected = CardumeRules.Display(config, CardumeRules.RawStrength(config, stats));
        Assert.True(expected > 0);
        Assert.Equal(expected, mine.Strength);
        Assert.Equal(expected, set.Value.Clan.Strength);
        var clan = save.Clans.KnownClans.Single();
        clan.Members.Add(new ClanMemberRecord { PlayerId = "x", Name = "X", FisherLevel = 200, FishSpeciesId = "lambari", FishLevel = 5, FishStrength = 1234 });
        var view = game.Clans.GetClan().Clan;
        Assert.Equal(expected + 1234, view.Strength);
        Assert.Equal(1234, view.Members.Single(m => !m.IsPlayer).FishStrength);
        Assert.Equal(expected, view.Members.Single(m => m.IsPlayer).FishStrength);

        // Change and remove.
        Assert.True(game.Clans.SetFish(fish[1]).Succeeded);
        Assert.Equal(fish[1], game.Clans.GetClan().Clan.MyFish.FishId);
        Assert.Equal(fish[0], save.CardumeSlots[0]);
        Assert.True(game.Clans.ClearFish().Succeeded);
        Assert.Null(game.Clans.GetClan().Clan.MyFish);
        Assert.Equal(1234, game.Clans.GetClan().Clan.Strength);
        Assert.Equal(2, game.Clans.GetFishCandidates().Count);
    }

    [Fact]
    public void A_fish_sold_or_listed_leaves_the_clan_s_Cardume()
    {
        var (game, _, _) = Player(dollars: Clans.Costs.CreateDollars);
        var fish = KeepFish(game, "lambari", 2);
        Assert.True(game.Clans.Create("Os Pescadores").Succeeded);
        var save = game.Session.Save;
        var me = save.Clans.KnownClans.Single().Members.Single();

        Assert.True(game.Clans.SetFish(fish[0]).Succeeded);
        Assert.True(game.Aquarium.SellFish(new[] { fish[0] }).Succeeded);
        Assert.Equal(0, me.FishId);
        Assert.Null(game.Clans.GetClan().Clan.MyFish);

        Assert.True(game.Clans.SetFish(fish[1]).Succeeded);
        var listed = game.Market.ListFish(fish[1], 500);
        Assert.True(listed.Succeeded, listed.ErrorMessage);
        Assert.Equal(0, me.FishId);

        // A fish gone some other way (an edited save) leaves at the next update, with a notice.
        var more = KeepFish(game, "lambari", 1);
        Assert.True(game.Clans.SetFish(more[0]).Succeeded);
        save.Aquarium.RemoveAll(f => f.Id == more[0]);
        Assert.True(game.Clans.Update().FishRemoved);
        Assert.Equal(0, me.FishId);
    }

    // ------------------------------------------------------------------ +1% XP

    [Fact]
    public void Members_get_1_percent_more_Fisher_XP_and_nobody_else_does()
    {
        var cost = Clans.Costs.CreateDollars;
        var (member, memberClock, _) = Player(dollars: cost);
        var (alone, aloneClock, _) = Player(dollars: cost);
        Assert.True(member.Clans.Create("Os Pescadores").Succeeded);
        Assert.True(member.Clans.GetClan().XpBonusActive);
        Assert.False(alone.Clans.GetClan().XpBonusActive);

        long Fish(LocalGame game, ManualClock clock, out long clanXp)
        {
            game.Fishing.StartFishing();
            long xp = 0;
            clanXp = 0;
            for (var i = 0; i < 300; i++)
            {
                clock.AdvanceSeconds(game.Session.Config.Fishing.OnlineCycleSeconds);
                var update = game.Fishing.Sync();
                xp += update.XpGained;
                clanXp += update.ClanXpGained;
            }

            return xp;
        }

        var withClan = Fish(member, memberClock, out var bonus);
        var without = Fish(alone, aloneClock, out var none);
        Assert.Equal(0, none);
        Assert.True(without > 0);
        Assert.Equal(without + bonus, withClan);

        // Exactly 1% over time: the fraction of each fish carries to the next one.
        Assert.InRange(bonus, (long)Math.Floor(without * 0.01) - 1, (long)Math.Floor(without * 0.01));
        Assert.True(bonus > 0);

        // Out of the clan, no bonus.
        Assert.True(member.Clans.Leave().Succeeded);
        Assert.Equal(0, ClanRules.TakeBonusXp(member.Session.Config, member.Session.Save, 1_000));
    }

    // ------------------------------------------------------------------ the daily ranking

    /// <summary>The next 00:00 in Brasília (03:00 UTC) after the start of the tests.</summary>
    private static long NextMidnight(ManualClock clock)
    {
        var start = DateTimeOffset.FromUnixTimeMilliseconds(clock.UtcNowMs).ToOffset(TimeSpan.FromHours(-3));
        return new DateTimeOffset(start.Date.AddDays(1), TimeSpan.FromHours(-3)).ToUnixTimeMilliseconds();
    }

    /// <summary>The player's clan with one fish, and two other clans: one stronger, one weaker or stronger as asked.</summary>
    private static (LocalGame Game, ManualClock Clock) RankedPlayer(GameConfig config, int othersAbove)
    {
        var (game, clock, _) = Player(config, dollars: config.Clans.Costs.CreateDollars);
        var fish = KeepFish(game, "lambari", 1, "large");
        Assert.True(game.Clans.Create("Os Pescadores").Succeeded);
        Assert.True(game.Clans.SetFish(fish[0]).Succeeded);
        var mine = game.Clans.GetClan().Clan.Strength;
        var save = game.Session.Save;
        for (var i = 0; i < 4; i++)
        {
            // othersAbove clans are stronger than the player's; the rest weaker.
            var each = i < othersAbove ? mine * 3 : Math.Max(1, mine / 4);
            save.Clans.KnownClans.Add(OtherClan("clan_o" + i, "Outro " + i, 2, each));
        }

        game.Clans.Update();
        return (game, clock);
    }

    [Fact]
    public void In_the_local_game_the_ranking_and_the_top_3_rewards_are_off()
    {
        var config = TestSupport.CertainCatchConfig();
        var (game, clock) = RankedPlayer(config, othersAbove: 0);
        var save = game.Session.Save;
        var before = (save.Dollars, save.Coins, save.Shells, save.Clothing.Fragments);

        var view = game.Clans.GetClan();
        Assert.False(view.Ranking.OnlineRewards);
        Assert.Empty(view.Ranking.Lines);
        Assert.Equal(3, view.Ranking.Rewards.Count);

        clock.UtcNowMs = NextMidnight(clock) + 60_000;
        var update = game.Clans.Update();
        Assert.Null(update.Paid);
        Assert.Equal((save.Dollars, save.Coins, save.Shells, save.Clothing.Fragments), before);
        Assert.Null(save.Clans.LastResult);
        Assert.Equal(ClanRules.DayOf(game.Session.Config, clock.UtcNowMs), save.Clans.RankingDay);
    }

    [Fact]
    public void With_the_online_flag_the_top_3_are_paid_once_at_00_00_Brasilia()
    {
        var config = Online;
        var (game, clock) = RankedPlayer(config, othersAbove: 2);
        var save = game.Session.Save;
        var view = game.Clans.GetClan();
        Assert.True(view.Ranking.OnlineRewards);
        Assert.Equal(3, view.Ranking.Lines.Single(l => l.IsMine).Rank);
        var expected = view.Ranking.Rewards.Single(r => r.Rank == 3);
        var before = (save.Dollars, save.Coins, save.Shells, save.Clothing.Fragments);

        // 23:59 in Brasília: still the same day, nothing paid.
        var midnight = NextMidnight(clock);
        clock.UtcNowMs = midnight - 1_000;
        Assert.Null(game.Clans.Update().Paid);
        Assert.Equal(before.Dollars, save.Dollars);

        // 00:00: the day closes and the 3rd clan's reward is paid once.
        clock.UtcNowMs = midnight;
        var paid = game.Clans.Update().Paid;
        Assert.NotNull(paid);
        Assert.Equal(3, paid.Rank);
        Assert.Equal(before.Dollars + expected.Dollars, save.Dollars);
        Assert.Equal(before.Coins + expected.Coins, save.Coins);
        Assert.Equal(before.Shells + expected.Shells, save.Shells);
        Assert.Equal(before.Fragments + expected.ClothingFragments, save.Clothing.Fragments);
        Assert.Equal(expected.BoatFragments, paid.BoatFragments);
        Assert.True(expected.Dollars > 0 && expected.Coins > 0 && expected.Shells > 0);
        Assert.Equal(3, save.Clans.LastResult.Rank);

        clock.AdvanceSeconds(3600);
        Assert.Null(game.Clans.Update().Paid);
        Assert.Equal(before.Dollars + expected.Dollars, save.Dollars);
    }

    [Fact]
    public void A_clan_below_the_top_3_gets_nothing()
    {
        var (game, clock) = RankedPlayer(Online, othersAbove: 3);
        var save = game.Session.Save;
        var dollars = save.Dollars;
        clock.UtcNowMs = NextMidnight(clock) + 1;
        Assert.Null(game.Clans.Update().Paid);
        Assert.Equal(dollars, save.Dollars);
        Assert.Equal(4, save.Clans.LastResult.Rank);
        Assert.Equal(0, save.Clans.LastResult.Dollars);
    }

    [Fact]
    public void The_day_closes_at_00_00_in_Brasilia_not_at_00_00_UTC()
    {
        var config = TestSupport.CertainCatchConfig();
        // 02:59:59 UTC is still 23:59:59 in Brasília; 03:00 UTC is 00:00 there.
        var day = ClanRules.DayOf(config, new DateTimeOffset(2026, 10, 14, 2, 59, 59, TimeSpan.Zero).ToUnixTimeMilliseconds());
        var next = ClanRules.DayOf(config, new DateTimeOffset(2026, 10, 14, 3, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds());
        Assert.Equal(day + 1, next);
        Assert.Equal(new DateTime(2026, 10, 13), ClanRules.DateOf(day));
        Assert.Equal(new DateTimeOffset(2026, 10, 14, 3, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds(),
            ClanRules.NextResetMs(config, new DateTimeOffset(2026, 10, 13, 12, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds()));
    }

    // ------------------------------------------------------------------ save v21 → v22 and huge numbers

    [Fact]
    public void A_version_21_save_opens_with_no_clan()
    {
        var (game, clock, dir) = Player();
        game.Session.Persist();
        var path = Path.Combine(dir, JsonFilePlayerRepository.SaveFileName);
        var json = JObject.Parse(File.ReadAllText(path));
        Assert.NotNull(json["clans"]);
        json["save_version"] = 21;
        json.Remove("clans");
        File.WriteAllText(path, json.ToString());
        File.Delete(Path.Combine(dir, JsonFilePlayerRepository.BackupFileName));

        clock.AdvanceSeconds(60);
        var (again, _, _) = TestSupport.NewGame(TestSupport.CertainCatchConfig(), dir, clock, skipTutorial: false);
        Assert.Equal(SaveLoadStatus.Loaded, again.Session.LoadStatus);
        Assert.Equal(22, PlayerSave.CurrentVersion);
        Assert.Equal(PlayerSave.CurrentVersion, again.Session.Save.SaveVersion);
        Assert.NotNull(again.Session.Save.Clans);
        Assert.Null(again.Session.Save.Clans.MyClanId);
        Assert.Empty(again.Session.Save.Clans.KnownClans);
        Assert.False(again.Clans.GetClan().InClan);
        Assert.Empty(SaveValidator.Validate(again.Session.Save));
        Assert.Contains("\"clans\"", File.ReadAllText(path));
    }

    [Fact]
    public void A_clan_that_points_nowhere_is_cleared_on_load()
    {
        var (game, clock, dir) = Player(dollars: Clans.Costs.CreateDollars);
        Assert.True(game.Clans.Create("Os Pescadores").Succeeded);
        game.Session.Save.Clans.KnownClans.Clear();
        game.Session.Persist();

        var (again, _, _) = TestSupport.NewGame(TestSupport.CertainCatchConfig(), dir, clock, skipTutorial: false);
        Assert.Equal(SaveLoadStatus.Loaded, again.Session.LoadStatus);
        Assert.Null(again.Session.Save.Clans.MyClanId);
        Assert.False(again.Clans.GetClan().InClan);
    }

    [Fact]
    public void Huge_numbers_never_wrap_around()
    {
        var config = Online;
        var save = new PlayerSave { PlayerId = "me", FisherLevel = 1000 };
        var clan = OtherClan("clan_big", "Grande", 3, long.MaxValue / 2);
        Assert.Equal(long.MaxValue, ClanRules.ClanStrength(config, save, clan));
        Assert.Equal(long.MaxValue, ClanRules.SlotPrice(config, 100_000));
        Assert.True(ClanRules.SlotPrice(config, 0) >= 1);

        save.Clans.KnownClans.Add(new ClanRecord { Id = "c", Name = "C", OwnerId = "me", Members = { new ClanMemberRecord { PlayerId = "me" } } });
        save.Clans.MyClanId = "c";
        Assert.InRange(ClanRules.TakeBonusXp(config, save, long.MaxValue), long.MaxValue / 101, long.MaxValue / 99);

        // Rewards on top of a wallet near the top stop at the top.
        var (game, clock) = RankedPlayer(config, othersAbove: 0);
        var s = game.Session.Save;
        s.Dollars = long.MaxValue - 1;
        s.Coins = long.MaxValue - 1;
        s.Shells = long.MaxValue - 1;
        clock.UtcNowMs = NextMidnight(clock) + 1;
        Assert.NotNull(game.Clans.Update().Paid);
        Assert.Equal(long.MaxValue, s.Dollars);
        Assert.Equal(long.MaxValue, s.Coins);
        Assert.Equal(long.MaxValue, s.Shells);
        Assert.Empty(SaveValidator.Validate(s));
    }
}
