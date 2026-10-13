using FishingIdle.GameService.Aquarium;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Market;
using FishingIdle.GameService.Missions;
using FishingIdle.GameService.Persistence;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>
/// The Missões (M24-T54, A-177, TD-057): 100 missions in missions.json, all visible from the start; progress read from the
/// save's state or from counters the services add to (online and offline); a mission completed stays completed, is
/// claimed once (SafeMath); the online-only ones never complete in the local game; save v24 fills the counters an old
/// save already shows and its reached missions appear ready to claim.
/// </summary>
public sealed class MissionsTests
{
    private static MissionView Mission(LocalGame game, string id) => game.Missions.GetMissions().Missions.Single(m => m.Id == id);

    private static bool Ready(LocalGame game, string id) => Mission(game, id).Ready;

    private static (LocalGame Game, ManualClock Clock, string Dir) NewGame(GameConfig config = null)
    {
        var (game, clock, dir) = TestSupport.NewGame(config, seed: TestSupport.FixedSeed);
        game.Session.DevToolsEnabled = true;
        return (game, clock, dir);
    }

    /// <summary>The config of the tests with an edit of missions.json.</summary>
    private static ConfigLoadResult WithMissions(Action<JObject> edit)
    {
        return TestSupport.ConfigWith(GameConfigLoader.MissionsFile, j =>
        {
            var root = JObject.Parse(j);
            edit(root);
            return root.ToString();
        }, certainCatch: true);
    }

    private static JObject MissionJson(JObject root, string id) => (JObject)root["missions"]!.First(m => (string)m["id"]! == id);

    // ------------------------------------------------------------------ config

    [Fact]
    public void The_config_has_the_100_approved_missions_in_order_with_growing_rewards()
    {
        var config = TestSupport.RealConfig();
        Assert.Contains(GameConfigLoader.MissionsFile, GameConfigLoader.RequiredFiles);
        var missions = config.Missions.Missions;
        Assert.Equal(100, missions.Count);
        Assert.Equal(Enumerable.Range(1, 100).Select(i => "mission_" + i.ToString("000")), missions.Select(m => m.Id));
        Assert.All(missions, m => Assert.Contains(m.Type, MissionTypes.All));

        // Moedas and Conchas in every mission, never less than the mission above; Dólares only in the hard ones.
        for (var i = 0; i < missions.Count; i++)
        {
            Assert.True(missions[i].Reward.Coins > 0 && missions[i].Reward.Shells > 0, missions[i].Id);
            if (i > 0)
            {
                Assert.True(missions[i].Reward.Coins >= missions[i - 1].Reward.Coins, missions[i].Id);
                Assert.True(missions[i].Reward.Shells >= missions[i - 1].Reward.Shells, missions[i].Id);
            }
        }

        var dollars = missions.Sum(m => m.Reward.Dollars);
        Assert.InRange(dollars, 100, 200);
        Assert.InRange(missions.Count(m => m.Reward.Dollars > 0), 15, 20);
        Assert.True(missions.Last().Reward.Coins > missions.First().Reward.Coins * 1000);

        // Only the clan's Boss day and the clan ranking need real players (A-177).
        Assert.Equal(new[] { "mission_092", "mission_099" }, missions.Where(m => m.OnlineOnly).Select(m => m.Id));
    }

    [Fact]
    public void The_validator_refuses_an_unknown_type_a_missing_parameter_a_bad_target_and_a_negative_reward()
    {
        var bad = WithMissions(root =>
        {
            MissionJson(root, "mission_001")["type"] = "catch_everything";
            MissionJson(root, "mission_005")["species_id"] = "dragao";
            MissionJson(root, "mission_006")["target"] = 0;
            MissionJson(root, "mission_007")["reward"]!["coins"] = -1;
            MissionJson(root, "mission_017")["map_id"] = "map_99";
            MissionJson(root, "mission_002")["id"] = "mission_003";
        });

        Assert.False(bad.Succeeded);
        Assert.Contains(bad.Errors, e => e.Contains("missions.json") && e.Contains("mission_001") && e.Contains("catch_everything"));
        Assert.Contains(bad.Errors, e => e.Contains("mission_005") && e.Contains("species_id"));
        Assert.Contains(bad.Errors, e => e.Contains("mission_006") && e.Contains("target"));
        Assert.Contains(bad.Errors, e => e.Contains("mission_007") && e.Contains("reward"));
        Assert.Contains(bad.Errors, e => e.Contains("mission_017") && e.Contains("map_id"));
        Assert.Contains(bad.Errors, e => e.Contains("mission_003"));
    }

    [Fact]
    public void Every_mission_reads_in_PT_BR_like_the_approved_list()
    {
        var (game, _, _) = NewGame();
        var view = game.Missions.GetMissions();
        Assert.Equal(100, view.Total);
        Assert.All(view.Missions, m => Assert.DoesNotContain("_", m.Text));

        string T(int n) => view.Missions[n - 1].Text;
        Assert.Equal("Pesque 10 peixes", T(1));
        Assert.Equal("Venda 10 peixes da Caixa de Pesca", T(2));
        Assert.Equal("Pesque 5 Lambaris", T(5));
        Assert.Equal("Junte 10.000 Moedas", T(11));
        Assert.Equal("Use uma isca Terra Viva", T(12));
        Assert.Equal("Pesque um peixe 4 estrelas", T(15));
        Assert.Equal("Monte a vara Ponta Selvagem", T(16));
        Assert.Equal("Libere o Rio Selvagem", T(17));
        Assert.Equal("Pesque o primeiro peixe Raro", T(20));
        Assert.Equal("Pesque todas as 10 espécies do Lago Sereno", T(30));
        Assert.Equal("Pesque 50 peixes Raros", T(36));
        Assert.Equal("Pesque um peixe com um gene perfeito (31)", T(42));
        Assert.Equal("Libere a Costa de Coral", T(56));
        Assert.Equal("Junte 1 milhão de Moedas", T(58));
        Assert.Equal("Monte um Cardume completo só com peixes Raros ou melhores", T(60));
        Assert.Equal("Alcance 100.000 de força no Cardume", T(62));
        Assert.Equal("Chegue ao top 100 do Ranking da Arena", T(63));
        Assert.Equal("Chegue à faixa máxima de dano do Boss Mundial", T(75));
        Assert.Equal("Ganhe um Leilão contra outros jogadores", T(85));
        Assert.Equal("Monte todos os 5 barcos", T(86));
        Assert.Equal("Junte 1 bilhão de Moedas", T(90));
        Assert.Equal("Registre todas as espécies na Enciclopédia", T(91));
        Assert.Equal("Pesque todas as espécies Lendárias", T(93));
        Assert.Equal("Pesque o Tubarão-boca-grande (Mítico)", T(94));
        Assert.Equal("Pesque 1 milhão de peixes", T(97));
        Assert.Equal("Pesque um Exemplar perfeito (os 4 genes em 31)", T(98));
        Assert.Equal("Chegue ao Nv.1000 de Pescador", T(100));
    }

    // ------------------------------------------------------------------ the start

    [Fact]
    public void A_new_player_sees_all_100_missions_none_ready_and_cannot_claim()
    {
        var (game, _, _) = NewGame();
        var view = game.Missions.GetMissions();

        Assert.Equal(100, view.Missions.Count);
        Assert.Equal(0, view.ReadyCount);
        Assert.Equal(0, game.Missions.ReadyCount);
        Assert.False(view.Online);
        Assert.Equal(ServiceError.MissionNotReady, game.Missions.Claim("mission_001").Error);
        Assert.Equal(ServiceError.MissionNothingToClaim, game.Missions.ClaimAll().Error);
        Assert.Equal(ServiceError.MissionNotFound, game.Missions.Claim("mission_999").Error);
        Assert.Equal("0 / 10", GameTextsProgress(Mission(game, "mission_001")));
    }

    private static string GameTextsProgress(MissionView m) => FishingIdle.Texts.GameTexts.Missions.Progress(m.Current, m.Target);

    // ------------------------------------------------------------------ fishing, online and offline

    [Fact]
    public void The_exact_target_completes_a_mission_and_the_claim_pays_once()
    {
        var (game, _, _) = NewGame();
        Assert.Equal(9, game.DevTools.GiveFish("tilapia", "small", 9).Value);
        game.Missions.TakeNewlyReady();
        Assert.False(Ready(game, "mission_001"));
        Assert.Equal(9, Mission(game, "mission_001").Current);

        Assert.Equal(1, game.DevTools.GiveFish("tilapia", "small", 1).Value);
        Assert.True(Ready(game, "mission_001"));
        Assert.True(game.Missions.ReadyCount >= 1);
        var notices = game.Missions.TakeNewlyReady();
        Assert.Contains(notices, n => n.Id == "mission_001" && n.Text == "Pesque 10 peixes");
        Assert.DoesNotContain(notices, n => n.Id == "mission_005");
        Assert.Empty(game.Missions.TakeNewlyReady());

        var save = game.Session.Save;
        var reward = game.Session.Config.Missions.Missions[0].Reward;
        var (coins, shells, dollars) = (save.Coins, save.Shells, save.Dollars);
        var claim = game.Missions.Claim("mission_001");
        Assert.True(claim.Succeeded, claim.ErrorMessage);
        Assert.Equal(1, claim.Value.Claimed);
        Assert.Equal(coins + reward.Coins, save.Coins);
        Assert.Equal(shells + reward.Shells, save.Shells);
        Assert.Equal(dollars + reward.Dollars, save.Dollars);

        var readyAfter = game.Missions.ReadyCount;
        Assert.Equal(ServiceError.MissionAlreadyClaimed, game.Missions.Claim("mission_001").Error);
        Assert.Equal(coins + reward.Coins, save.Coins);
        var claimed = Mission(game, "mission_001");
        Assert.True(claimed.Claimed);
        Assert.False(claimed.Ready);
        Assert.Equal(readyAfter, game.Missions.GetMissions().ReadyCount);
    }

    [Fact]
    public void Online_fishing_counts_every_catch_with_its_size_and_stars()
    {
        var (game, clock, _) = NewGame();
        Assert.True(game.Fishing.StartFishing().Succeeded);
        var catches = TestSupport.PlayFor(game, clock, 1200, stepSeconds: 10);

        var counters = game.Session.Save.Missions.Counters;
        Assert.True(catches.Count >= 100);
        Assert.Equal(catches.Count, counters.Where(c => c.Key.StartsWith(MissionCounterKeys.CatchSizePrefix)).Sum(c => c.Value));
        Assert.Equal(catches.Count, counters.Where(c => c.Key.StartsWith(MissionCounterKeys.CatchStarsPrefix)).Sum(c => c.Value));
        Assert.True(Ready(game, "mission_001"));
        Assert.True(Ready(game, "mission_006"));
        Assert.Contains(game.Missions.TakeNewlyReady(), n => n.Id == "mission_006");
    }

    [Fact]
    public void Offline_fishing_counts_when_the_game_opens_again()
    {
        var (game, clock, dir) = NewGame();
        Assert.True(game.Fishing.StartFishing().Succeeded);
        game.Fishing.MarkSeen();
        clock.AdvanceSeconds(3 * 3600);

        var (again, _, _) = TestSupport.NewGame(saveDir: dir, clock: clock, skipTutorial: false);
        again.Fishing.Sync();
        var save = again.Session.Save;
        Assert.True(save.Stats.TotalCatches >= 100);
        Assert.Equal(save.Stats.TotalCatches, save.Missions.Counters.Where(c => c.Key.StartsWith(MissionCounterKeys.CatchSizePrefix)).Sum(c => c.Value));
        Assert.True(Ready(again, "mission_006"));
    }

    [Fact]
    public void Gene_stars_perfect_genes_perfect_specimens_and_size_records_count_from_each_catch()
    {
        var (game, _, _) = NewGame();
        var config = game.Session.Config;
        var save = game.Session.Save;
        MissionTracker.OnCatch(config, save, new BoxCatch { SpeciesId = "lambari", SizeMm = 100, SizeCategoryId = "large", Genes = new[] { 31, 31, 31, 31 } }, recordBeaten: true);
        game.Session.Persist();

        Assert.True(Ready(game, "mission_010"));   // Grande
        Assert.True(Ready(game, "mission_015"));   // 4 estrelas: a 5-star fish counts
        Assert.True(Ready(game, "mission_041"));   // 5 estrelas
        Assert.True(Ready(game, "mission_042"));   // gene perfeito
        Assert.True(Ready(game, "mission_098"));   // Exemplar perfeito
        Assert.Equal(1, Mission(game, "mission_095").Current);
        Assert.Equal(1, Mission(game, "mission_072").Current);

        MissionTracker.OnCatch(config, save, new BoxCatch { SpeciesId = "lambari", SizeMm = 100, SizeCategoryId = "small", Genes = new[] { 0, 0, 0, 31 } }, recordBeaten: true);
        Assert.Equal(1, save.Missions.RecordSpecies.Count);
        Assert.Equal(2, MissionTracker.Get(save, MissionCounterKeys.CatchPerfectGene));
        Assert.Equal(1, MissionTracker.Get(save, MissionCounterKeys.CatchPerfectSpecimen));
    }

    // ------------------------------------------------------------------ the state

    [Fact]
    public void State_missions_complete_from_the_level_the_Encyclopedia_the_maps_and_the_pass()
    {
        var (game, _, _) = NewGame();
        Assert.True(game.DevTools.SetFisherLevel(100).Succeeded);
        foreach (var id in new[] { "mission_004", "mission_013", "mission_017", "mission_029", "mission_032", "mission_034" })
        {
            Assert.True(Ready(game, id), id);
        }

        Assert.False(Ready(game, "mission_059"));
        Assert.False(Ready(game, "mission_028"));
        Assert.True(game.Pass.ClaimAll().Succeeded);
        Assert.True(Ready(game, "mission_028"));   // 10 recompensas do Passe

        var species = game.Session.Config.FishCatalog.Species.Take(10).Select(s => s.Id).ToList();
        foreach (var s in species)
        {
            game.DevTools.GiveFish(s, null, 1);
        }

        Assert.True(Ready(game, "mission_014"));
        Assert.Equal(10, Mission(game, "mission_057").Current);
    }

    [Fact]
    public void A_completed_mission_stays_completed_when_the_state_goes_back_down()
    {
        var (game, _, _) = NewGame();
        var save = game.Session.Save;
        save.Coins = 9_999;
        game.Session.Persist();
        Assert.False(Ready(game, "mission_011"));

        // Reached for a moment (Moedas before a purchase): the write in between marks it.
        save.Coins = 10_000;
        game.Session.Persist();
        save.Coins = 0;
        game.Session.Persist();

        var m = Mission(game, "mission_011");
        Assert.True(m.Ready);
        Assert.Equal(m.Target, m.Current);
        Assert.True(game.Missions.Claim("mission_011").Succeeded);
    }

    [Fact]
    public void Aquarium_keep_feed_level_and_Cardume_missions_follow_the_services()
    {
        var (game, _, _) = NewGame();
        game.DevTools.GiveFish("lambari", null, 12);
        var box = game.Fishing.GetFishingBox().Select(c => c.CatchId).ToList();
        Assert.True(game.Aquarium.KeepCatches(box.Take(8).ToList()).Succeeded);
        Assert.True(Ready(game, "mission_003"));

        var fish = game.Aquarium.GetAquarium(AquariumSort.Newest).Fish.Select(f => f.FishId).ToList();
        Assert.True(game.Aquarium.Feed(fish[0], box.Skip(8).Take(2).ToList(), Array.Empty<long>()).Succeeded);
        Assert.True(Ready(game, "mission_007"));
        Assert.Equal(1, MissionTracker.Get(game.Session.Save, MissionCounterKeys.FishFed));

        for (var i = 0; i < 3; i++)
        {
            Assert.True(game.Cardume.SetSlot(i + 1, fish[i + 1]).Succeeded);
        }

        Assert.True(Ready(game, "mission_009"));

        // Six Raros at Nv.100 in the Cardume: "só Raros ou melhores" and "todos no Nv.100".
        game.DevTools.GiveFish("aruana", null, 6);
        var rares = game.Fishing.GetFishingBox().Where(c => c.SpeciesId == "aruana").Select(c => c.CatchId).ToList();
        Assert.True(game.Aquarium.KeepCatches(rares).Succeeded);
        var save = game.Session.Save;
        var rareFish = save.Aquarium.Where(f => f.SpeciesId == "aruana").ToList();
        foreach (var f in rareFish)
        {
            f.Level = 100;
        }

        for (var i = 0; i < 6; i++)
        {
            Assert.True(game.Cardume.SetSlot(i + 1, rareFish[i].Id).Succeeded);
        }

        Assert.True(Ready(game, "mission_060"));
        Assert.True(Ready(game, "mission_070"));
        Assert.True(Ready(game, "mission_096"));
    }

    [Fact]
    public void Sales_baits_Dollars_Expeditions_and_the_clan_count_through_their_services()
    {
        var (game, clock, _) = NewGame();
        var save = game.Session.Save;
        game.DevTools.GiveFish("lambari", null, 16);
        var box = game.Fishing.GetFishingBox().Select(c => c.CatchId).ToList();
        Assert.True(game.Fishing.SellCatches(box.Take(10).ToList()).Succeeded);
        Assert.True(Ready(game, "mission_002"));
        Assert.Equal(10, MissionTracker.Get(save, MissionCounterKeys.BoxSold));

        // A bait charge spent fishing.
        save.BaitCharges["bait_01"] = 5;
        save.ActiveBaitId = "bait_01";
        Assert.True(game.Fishing.StartFishing().Succeeded);
        TestSupport.PlayFor(game, clock, 30, stepSeconds: 5);
        Assert.True(game.Fishing.StopFishing().Succeeded);
        Assert.True(MissionTracker.Get(save, MissionCounterKeys.BaitPrefix + "bait_01") >= 1);
        Assert.True(Ready(game, "mission_012"));

        // Dólares spent on the VIP.
        save.Dollars = 1_000;
        Assert.True(game.Vip.BuyVip().Succeeded);
        Assert.Equal(game.Session.Config.Economy.Vip.PriceDollars, MissionTracker.Get(save, MissionCounterKeys.DollarsSpent));
        Assert.True(Ready(game, "mission_065"));

        // An Expedition sent and finished.
        Assert.True(game.Aquarium.KeepCatches(game.Fishing.GetFishingBox().Select(c => c.CatchId).Take(1).ToList()).Succeeded);
        Assert.True(game.Cardume.SetSlot(1, save.Aquarium[0].Id).Succeeded);
        Assert.True(game.Expeditions.Start("exp_30m").Succeeded);
        Assert.True(Ready(game, "mission_021"));
        clock.AdvanceSeconds(3600);
        Assert.NotNull(game.Expeditions.Update());
        Assert.Equal(1, MissionTracker.Get(save, MissionCounterKeys.ExpeditionsCompleted));

        // A clan at Nv.100, with a fish in the clan's Cardume.
        Assert.True(game.DevTools.SetFisherLevel(100).Succeeded);
        Assert.True(game.Clans.Create("Os Pescadores").Succeeded);
        Assert.True(Ready(game, "mission_033"));
        Assert.True(game.Clans.SetFish(save.Aquarium[0].Id).Succeeded);
        Assert.True(Ready(game, "mission_044"));
    }

    [Fact]
    public void Arena_duels_wins_streaks_opponents_ranks_and_profile_visits_count()
    {
        var (game, clock, _) = NewGame();
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 1800, stepSeconds: 30);
        game.Fishing.StopFishing();
        game.Aquarium.KeepCatches(game.Fishing.GetFishingBox().Take(6).Select(c => c.CatchId).ToList());
        var ids = game.Aquarium.GetAquarium(AquariumSort.Size).Fish.Select(f => f.FishId).ToList();
        for (var i = 0; i < 6; i++)
        {
            game.Cardume.SetSlot(i + 1, ids[i]);
        }

        var opponent = game.Arena.GetArena().Opponents[0].ParticipantId;
        Assert.Equal(ServiceError.OpponentNotFound, game.Arena.VisitProfile("player").Error);
        Assert.Equal(ServiceError.OpponentNotFound, game.Arena.VisitProfile("bot:no_such").Error);
        Assert.True(game.Arena.VisitProfile(opponent).Succeeded);
        Assert.True(Ready(game, "mission_022"));

        var report = game.Arena.Attack(0);
        Assert.True(report.Succeeded, report.ErrorMessage);
        var save = game.Session.Save;
        Assert.Equal(1, MissionTracker.Get(save, MissionCounterKeys.ArenaDuels));
        Assert.Equal(report.Value.PlayerWon ? 1 : 0, MissionTracker.Get(save, MissionCounterKeys.ArenaWins));
        Assert.Contains(opponent, save.Missions.ArenaOpponents);
        Assert.True(Ready(game, "mission_018"));
        Assert.True(MissionTracker.Get(save, MissionCounterKeys.ArenaBestRank) > 0);

        // The streak: three wins in a row, a loss resets the current one, the best stays.
        MissionTracker.OnArenaAttack(save, "bot:1", true, 50, 40);
        MissionTracker.OnArenaAttack(save, "bot:2", true, 40, 30);
        MissionTracker.OnArenaAttack(save, "bot:3", true, 30, 30);
        MissionTracker.OnArenaAttack(save, "bot:3", false, 30, 30);
        Assert.Equal(0, MissionTracker.Get(save, MissionCounterKeys.ArenaStreak));
        Assert.True(MissionTracker.Get(save, MissionCounterKeys.ArenaBestStreak) >= 3);
        Assert.True(MissionTracker.Get(save, MissionCounterKeys.ArenaStrongerWins) >= 2);
        game.Session.Persist();
        Assert.True(Ready(game, "mission_047"));
        Assert.True(Ready(game, "mission_061"));

        // Top 100 of the Arena ranking: the player's place in today's ranking.
        var ranking = save.Arena.Ranking;
        ranking.Remove("player");
        ranking.Insert(Math.Min(50, ranking.Count), "player");
        game.Session.Persist();
        Assert.True(Ready(game, "mission_063"));
        Assert.False(Ready(game, "mission_077"));
    }

    [Fact]
    public void Market_listings_purchases_sales_bids_and_auctions_won_count()
    {
        var (game, _, _) = NewGame();
        var save = game.Session.Save;
        save.Coins = 100_000_000;
        game.DevTools.GiveFish("lambari", null, 2);
        Assert.True(game.Aquarium.KeepCatches(game.Fishing.GetFishingBox().Select(c => c.CatchId).ToList()).Succeeded);
        Assert.True(game.Market.ListFish(save.Aquarium[0].Id, 5_000).Succeeded);
        Assert.True(Ready(game, "mission_024"));

        var fish = game.Market.Search(new MarketQuery()).Where(l => l.Goods.IsFish).OrderBy(l => l.PriceCoins).First();
        Assert.True(game.Market.Buy(fish.ListingId).Succeeded);
        Assert.True(Ready(game, "mission_025"));
        Assert.True(Ready(game, "mission_069"));

        var auction = game.Auctions.GetAuctions().Open.OrderBy(a => a.MinNextBidCoins).First();
        Assert.True(game.Auctions.PlaceBid(auction.AuctionId, auction.MinNextBidCoins).Succeeded);
        Assert.True(Ready(game, "mission_050"));

        // A rare fish sold to another player, and auctions won (contested = 2 bids or more).
        var config = game.Session.Config;
        MissionTracker.OnMarketSold(config, save, new MarketGoods { Kind = MarketGoods.KindFish, Fish = new FishInstance { SpeciesId = "aruana" } });
        MissionTracker.OnMarketSold(config, save, new MarketGoods { Kind = MarketGoods.KindRod, Rod = new InventoryItem { RodId = "rod_01" } });
        MissionTracker.OnAuctionWon(save, 1);
        Assert.True(MissionTracker.Get(save, MissionCounterKeys.MarketSoldFish) >= 1);
        Assert.Equal(0, MissionTracker.Get(save, MissionCounterKeys.AuctionsWonContested));
        MissionTracker.OnAuctionWon(save, 3);
        game.Session.Persist();
        Assert.True(Ready(game, "mission_049"));
        Assert.True(Ready(game, "mission_078"));
        Assert.True(Ready(game, "mission_085"));
        Assert.Equal(2, Mission(game, "mission_081").Current);
    }

    [Fact]
    public void Boss_attacks_count_the_days_and_the_best_band()
    {
        var (game, _, _) = NewGame();
        var save = game.Session.Save;
        MissionTracker.OnBossAttacks(save, 100, 5, 2);
        MissionTracker.OnBossAttacks(save, 100, 5, 3);
        Assert.Equal(1, MissionTracker.Get(save, MissionCounterKeys.BossDays));
        Assert.Equal(10, MissionTracker.Get(save, MissionCounterKeys.BossAttacks));
        for (var day = 101; day <= 106; day++)
        {
            MissionTracker.OnBossAttacks(save, day, 1, 1);
        }

        MissionTracker.OnBossAttacks(save, 107, 0, 6);   // no attack: nothing counts
        game.Session.Persist();
        Assert.Equal(7, MissionTracker.Get(save, MissionCounterKeys.BossDays));
        Assert.Equal(3, MissionTracker.Get(save, MissionCounterKeys.BossBestBand));
        Assert.True(Ready(game, "mission_043"));
        Assert.True(Ready(game, "mission_045"));
        Assert.True(Ready(game, "mission_074"));
        Assert.False(Ready(game, "mission_075"));
        Assert.Equal(game.Session.Config.WorldBoss.Bands.Count, Mission(game, "mission_075").Target);
    }

    // ------------------------------------------------------------------ online only

    [Fact]
    public void Online_only_missions_never_complete_in_the_local_game()
    {
        var (game, _, _) = NewGame();
        var save = game.Session.Save;
        save.FisherLevel = 100;
        Assert.True(game.Clans.Create("Os Pescadores").Succeeded);
        save.Clans.LastResult = new ClanRankingResult { Day = 1, ClanName = "Os Pescadores", Rank = 1 };
        MissionTracker.OnBossAttacks(save, 100, 3, 1);
        game.Session.Persist();

        var top = Mission(game, "mission_099");
        Assert.True(top.OnlineOnly);
        Assert.False(top.Ready);
        Assert.Equal(0, top.Current);
        Assert.False(Ready(game, "mission_092"));

        // The flag works for any type: a level mission marked online-only stays open at that level.
        var config = WithMissions(root => MissionJson(root, "mission_004")["online_only"] = true);
        Assert.True(config.Succeeded, string.Join("\n", config.Errors));
        var (local, _, _) = NewGame(config.Config);
        Assert.True(local.DevTools.SetFisherLevel(10).Succeeded);
        Assert.False(Ready(local, "mission_004"));
        Assert.True(Ready(local, "mission_013") == false && Ready(local, "mission_017"));
    }

    // ------------------------------------------------------------------ big numbers and persistence

    [Fact]
    public void Claims_and_counters_stop_at_the_top_instead_of_wrapping()
    {
        var (game, _, _) = NewGame();
        var save = game.Session.Save;
        save.Coins = long.MaxValue - 10;
        save.Shells = long.MaxValue - 1;
        game.Session.Persist();
        Assert.True(Ready(game, "mission_011"));
        Assert.True(Ready(game, "mission_058"));
        Assert.True(Ready(game, "mission_090"));
        Assert.True(Ready(game, "mission_031"));

        var all = game.Missions.ClaimAll();
        Assert.True(all.Succeeded, all.ErrorMessage);
        Assert.Equal(long.MaxValue, save.Coins);
        Assert.Equal(long.MaxValue, save.Shells);

        save.Missions.Counters[MissionCounterKeys.BoxSold] = long.MaxValue - 1;
        MissionTracker.OnBoxSold(save, 10);
        Assert.Equal(long.MaxValue, MissionTracker.Get(save, MissionCounterKeys.BoxSold));
        game.Session.Persist();
        Assert.Empty(SaveValidator.Validate(save));
    }

    [Fact]
    public void Claimed_missions_and_counters_survive_closing_and_opening_the_game()
    {
        var (game, clock, dir) = NewGame();
        game.DevTools.GiveFish("lambari", null, 10);
        Assert.True(game.Fishing.SellCatches(game.Fishing.GetFishingBox().Select(c => c.CatchId).ToList()).Succeeded);
        Assert.True(game.Missions.Claim("mission_001").Succeeded);
        Assert.True(Ready(game, "mission_002"));

        var (again, _, _) = TestSupport.NewGame(saveDir: dir, clock: clock);
        Assert.True(Mission(again, "mission_001").Claimed);
        Assert.True(Ready(again, "mission_002"));
        Assert.Equal(10, MissionTracker.Get(again.Session.Save, MissionCounterKeys.BoxSold));
        Assert.Equal(ServiceError.MissionAlreadyClaimed, again.Missions.Claim("mission_001").Error);
        Assert.True(again.Missions.Claim("mission_002").Succeeded);
        Assert.Contains("\"missions\"", File.ReadAllText(Path.Combine(dir, JsonFilePlayerRepository.SaveFileName)));
    }

    // ------------------------------------------------------------------ save v24

    [Fact]
    public void A_v23_save_opens_with_the_counters_its_state_shows_and_its_reached_missions_ready()
    {
        var (game, clock, dir) = NewGame();
        var save = game.Session.Save;
        game.DevTools.GiveFish("lambari", null, 3);
        game.DevTools.GiveFish("aruana", "exceptional", 1);
        var box = game.Fishing.GetFishingBox().Select(c => c.CatchId).ToList();
        Assert.True(game.Aquarium.KeepCatches(box.Take(2).ToList()).Succeeded);
        save.Aquarium[0].Level = 25;
        save.Aquarium[0].InvestedXp = 500;
        save.FisherLevel = 60;
        save.FisherXp = 0;
        save.Coins = 2_000_000;
        save.Stats.FishSold = 20;
        save.ExpeditionsStarted = 12;
        save.Arena.History.Add(new BattleRecord { Kind = "attack", OpponentId = "bot:7", PlayerWon = true, RankBefore = 90, RankAfter = 80 });
        save.Arena.History.Add(new BattleRecord { Kind = "attack", OpponentId = "bot:8", PlayerWon = false, RankBefore = 90, RankAfter = 90 });
        game.Session.Persist();

        // Rewrite the file as version 23: no "missions".
        var path = Path.Combine(dir, JsonFilePlayerRepository.SaveFileName);
        var json = JObject.Parse(File.ReadAllText(path));
        json["save_version"] = 23;
        json.Remove("missions");
        File.WriteAllText(path, json.ToString());
        File.Delete(Path.Combine(dir, JsonFilePlayerRepository.BackupFileName));

        var opened = LocalGame.Start(TestSupport.CertainCatchConfig(), new JsonFilePlayerRepository(dir), clock, _ => { });
        Assert.True(opened.Succeeded, string.Join("\n", opened.Errors));
        var again = opened.Game;
        Assert.Equal(SaveLoadStatus.Loaded, again.Session.LoadStatus);
        var migrated = again.Session.Save;
        Assert.Equal(PlayerSave.CurrentVersion, migrated.SaveVersion);
        Assert.Equal(24, PlayerSave.CurrentVersion);
        Assert.False(migrated.Missions.SeedPending);
        Assert.Empty(SaveValidator.Validate(migrated));

        // Counters filled from what the old save shows.
        Assert.Equal(20, MissionTracker.Get(migrated, MissionCounterKeys.BoxSold));
        Assert.Equal(12, MissionTracker.Get(migrated, MissionCounterKeys.ExpeditionsCompleted));
        Assert.Equal(1, MissionTracker.Get(migrated, MissionCounterKeys.FishFed));
        Assert.Equal(2, MissionTracker.Get(migrated, MissionCounterKeys.ArenaDuels));
        Assert.Equal(1, MissionTracker.Get(migrated, MissionCounterKeys.ArenaWins));
        Assert.True(MissionTracker.Get(migrated, MissionCounterKeys.CatchSizePrefix + "exceptional") >= 1);

        // The missions the state proves are ready to claim, with no notice flood (the badge shows them).
        foreach (var id in new[] { "mission_002", "mission_003", "mission_004", "mission_007", "mission_011", "mission_013", "mission_017", "mission_018",
                     "mission_029", "mission_032", "mission_039", "mission_040", "mission_048", "mission_058", "mission_061" })
        {
            Assert.True(Ready(again, id), id);
        }

        Assert.False(Ready(again, "mission_034"));
        Assert.True(again.Missions.ReadyCount >= 15);
        Assert.Empty(again.Missions.TakeNewlyReady());
        Assert.True(again.Missions.ClaimAll().Succeeded);

        // Opening again does not seed twice nor pay again.
        var (third, _, _) = TestSupport.NewGame(saveDir: dir, clock: clock);
        Assert.Equal(20, MissionTracker.Get(third.Session.Save, MissionCounterKeys.BoxSold));
        Assert.Equal(0, third.Missions.ReadyCount);
    }
}
