using FishingIdle.GameService.Config;
using FishingIdle.GameService.Persistence;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>
/// M24-T46 (A-172, TD-053), the owner's decision of 12/10/2026: "Não vai ter tripulação, retira isso do jogo", with the
/// general Upgrades too. A save from before (versions 13 to 20) opens, gets back the Moedas its Crew and Upgrades cost
/// (economy.json → crew_removal_refund, the old price curves) and no longer carries them. The game never fails to open an
/// old save (TD-050).
/// </summary>
public sealed class CrewRemovalTests
{
    private static readonly GameConfig Config = TestSupport.RealConfig();

    /// <summary>A saved game rewritten as version 20 with <paramref name="crew"/> and <paramref name="upgrades"/> (null = field absent).</summary>
    private static (string Dir, ManualClock Clock, long Coins) WriteVersion20(JObject crew, JObject upgrades, long coins = 1_000)
    {
        var (game, clock, dir) = TestSupport.NewGame(Config);
        game.Session.Save.Coins = coins;
        game.Session.Persist();

        var path = Path.Combine(dir, JsonFilePlayerRepository.SaveFileName);
        var json = JObject.Parse(File.ReadAllText(path));
        json["save_version"] = 20;
        json.Remove("crew");
        json.Remove("upgrades");
        if (crew != null)
        {
            json["crew"] = crew;
        }

        if (upgrades != null)
        {
            json["upgrades"] = upgrades;
        }

        File.WriteAllText(path, json.ToString());
        File.Delete(Path.Combine(dir, JsonFilePlayerRepository.BackupFileName));
        return (dir, clock, coins);
    }

    private static JObject Crew(params (string Id, long Units)[] units)
    {
        var map = new JObject();
        foreach (var (id, count) in units)
        {
            map[id] = count;
        }

        return new JObject
        {
            ["units"] = map, ["last_credited_at_ms"] = TestSupport.StartMs, ["coins_carry"] = 0.5, ["xp_carry"] = 0.25,
            ["coins_earned"] = 999, ["xp_earned"] = 9,
        };
    }

    private static JObject Upgrades(params (string Id, int Level)[] levels)
    {
        var map = new JObject();
        foreach (var (id, level) in levels)
        {
            map[id] = level;
        }

        return new JObject { ["levels"] = map };
    }

    private static LocalGame Open(string dir, ManualClock clock) => TestSupport.NewGame(Config, dir, clock, skipTutorial: false).Game;

    [Fact]
    public void The_save_is_version_21_and_the_config_keeps_the_old_prices_only_for_the_refund()
    {
        Assert.Equal(21, PlayerSave.CurrentVersion);
        var refund = Config.Economy.CrewRemovalRefund;
        Assert.NotNull(refund);
        Assert.Equal(10, refund.CrewMembers.Count);
        Assert.Equal(5, refund.CrewUpgradeTiers.Count);
        Assert.Equal(new[] { "port_radio", "fair_customers", "cooler_box", "school_sonar", "good_tide" }, refund.GeneralUpgrades.Select(g => g.Id));
        Assert.DoesNotContain("crew.json", GameConfigLoader.RequiredFiles);
        Assert.DoesNotContain("upgrades.json", GameConfigLoader.RequiredFiles);
        Assert.False(File.Exists(Path.Combine(TestSupport.ConfigDirectory, "crew.json")));
        Assert.False(File.Exists(Path.Combine(TestSupport.ConfigDirectory, "upgrades.json")));
    }

    [Fact]
    public void A_version_20_save_gets_back_every_Moeda_its_Crew_and_Upgrades_cost()
    {
        // 10 Ajudantes da Isca: 15 × (1,1^10 − 1) ÷ 0,1 = 239. 3 Canoeiros: 160 × (1,097^3 − 1) ÷ 0,097 = 528.
        // Anzol Afiado do Ajudante (tier 1): 10 × 15 × 1,1^1 = 165. Linha Reforçada do Canoeiro (tier 2): 50 × 160 × 1,097^10 = 20.191.
        // Freguesia na Feira Nv.2: 1.500 + 6.000 = 7.500. Rádio do Porto Nv.1: 20.000.
        var (dir, clock, coins) = WriteVersion20(
            Crew(("crew_01", 10), ("crew_02", 3)),
            Upgrades(("crew_01_up_1", 1), ("crew_02_up_2", 1), ("fair_customers", 2), ("port_radio", 1)));

        var game = Open(dir, clock);

        Assert.Equal(SaveLoadStatus.Loaded, game.Session.LoadStatus);
        var refund = game.Session.CrewRefund;
        Assert.NotNull(refund);
        Assert.True(refund.HadData);
        Assert.Equal(13, refund.UnitsRefunded);
        Assert.Equal(5, refund.UpgradeLevelsRefunded);
        Assert.Equal(239 + 528, refund.CrewCoins);
        Assert.Equal(165 + 20_191 + 7_500 + 20_000, refund.UpgradeCoins);
        Assert.Equal(48_623, refund.CoinsRefunded);
        Assert.Equal(coins + 48_623, game.Player.GetPlayer().Coins);
        Assert.Equal(PlayerSave.CurrentVersion, game.Session.Save.SaveVersion);
        Assert.Null(game.Session.Save.RetiredCrew);
        Assert.Null(game.Session.Save.RetiredUpgrades);

        // The file written after opening has neither field, and opening again refunds nothing more.
        var json = JObject.Parse(File.ReadAllText(Path.Combine(dir, JsonFilePlayerRepository.SaveFileName)));
        Assert.Equal(21, (int)json["save_version"]!);
        Assert.Null(json["crew"]);
        Assert.Null(json["upgrades"]);

        var again = Open(dir, clock);
        Assert.Null(again.Session.CrewRefund);
        Assert.Equal(coins + 48_623, again.Player.GetPlayer().Coins);
    }

    [Fact]
    public void A_version_20_save_that_never_hired_anyone_opens_with_the_same_Moedas()
    {
        var (dir, clock, coins) = WriteVersion20(Crew(), Upgrades());

        var game = Open(dir, clock);

        Assert.Equal(SaveLoadStatus.Loaded, game.Session.LoadStatus);
        Assert.True(game.Session.CrewRefund.HadData);
        Assert.Equal(0, game.Session.CrewRefund.CoinsRefunded);
        Assert.Equal(coins, game.Player.GetPlayer().Coins);
        Assert.Null(JObject.Parse(File.ReadAllText(Path.Combine(dir, JsonFilePlayerRepository.SaveFileName)))["crew"]);
    }

    [Fact]
    public void A_version_20_save_without_the_fields_opens_and_nothing_is_refunded()
    {
        var (dir, clock, coins) = WriteVersion20(null, null);

        var game = Open(dir, clock);

        Assert.Equal(SaveLoadStatus.Loaded, game.Session.LoadStatus);
        Assert.Null(game.Session.CrewRefund);
        Assert.Equal(coins, game.Player.GetPlayer().Coins);
        Assert.Equal(PlayerSave.CurrentVersion, game.Session.Save.SaveVersion);
    }

    [Fact]
    public void Unknown_ids_negative_counts_and_levels_above_the_old_max_never_give_extra_Moedas()
    {
        // An edited save: a member that never existed, an upgrade tier that never existed, a negative count, and the
        // Freguesia na Feira at Nv.50 (its max was 10: only 10 levels are refunded).
        var (dir, clock, coins) = WriteVersion20(
            Crew(("crew_99", 50), ("crew_01", -5), ("crew_02", 0)),
            Upgrades(("crew_01_up_9", 1), ("feira_magica", 3), ("crew_01_up_x", 1), ("fair_customers", 50)));

        var game = Open(dir, clock);

        var fairMax = Config.Economy.CrewRemovalRefund.GeneralUpgrades.Single(g => g.Id == "fair_customers");
        var expected = Enumerable.Range(0, fairMax.MaxLevel).Sum(n => (long)Math.Round(fairMax.BaseCost * Math.Pow(fairMax.CostGrowth, n), MidpointRounding.AwayFromZero));
        Assert.Equal(SaveLoadStatus.Loaded, game.Session.LoadStatus);
        Assert.Equal(4, game.Session.CrewRefund.UnknownIds);
        Assert.Equal(0, game.Session.CrewRefund.CrewCoins);
        Assert.Equal(expected, game.Session.CrewRefund.UpgradeCoins);
        Assert.Equal(coins + expected, game.Player.GetPlayer().Coins);
    }

    [Fact]
    public void A_huge_Crew_saturates_at_the_top_instead_of_wrapping()
    {
        // A billion Navios-Fábrica (an edited save) cost more than a long holds: the wallet stops at the top (SafeMath).
        var (dir, clock, _) = WriteVersion20(Crew(("crew_10", 1_000_000_000), ("crew_09", 1_000_000_000)), Upgrades(("port_radio", 10)), coins: long.MaxValue - 5);

        var game = Open(dir, clock);

        Assert.Equal(SaveLoadStatus.Loaded, game.Session.LoadStatus);
        Assert.Equal(long.MaxValue, game.Session.CrewRefund.CrewCoins);
        Assert.Equal(long.MaxValue, game.Session.CrewRefund.CoinsRefunded);
        Assert.Equal(long.MaxValue, game.Player.GetPlayer().Coins);
        Assert.Empty(SaveValidator.Validate(game.Session.Save));
    }

    [Fact]
    public void A_config_without_the_refund_prices_still_opens_an_old_save()
    {
        var config = TestSupport.ConfigWith(GameConfigLoader.EconomyFile, text =>
        {
            var json = JObject.Parse(text);
            json.Remove("crew_removal_refund");
            return json.ToString();
        });
        Assert.True(config.Succeeded, string.Join("\n", config.Errors));
        var (dir, clock, coins) = WriteVersion20(Crew(("crew_01", 10)), Upgrades(("fair_customers", 1)));

        var game = TestSupport.NewGame(config.Config, dir, clock, skipTutorial: false).Game;

        Assert.Equal(SaveLoadStatus.Loaded, game.Session.LoadStatus);
        Assert.Equal(0, game.Session.CrewRefund.CoinsRefunded);
        Assert.Equal(2, game.Session.CrewRefund.UnknownIds);
        Assert.Equal(coins, game.Player.GetPlayer().Coins);
        Assert.Null(game.Session.Save.RetiredCrew);
    }

    [Fact]
    public void A_broken_refund_price_list_is_refused_with_a_message_in_portuguese()
    {
        var config = TestSupport.ConfigWith(GameConfigLoader.EconomyFile, text => text.Replace("\"cost_growth\": 1.1\n", "\"cost_growth\": 0.9\n"));

        Assert.False(config.Succeeded);
        Assert.Contains(config.Errors, e => e.Contains("crew_removal_refund.crew_members (crew_01)"));
    }

    [Fact]
    public void Nothing_in_the_game_sells_fish_above_the_NPC_price_any_more()
    {
        // The Freguesia na Feira and the Maré Boa are gone: the Fishing Box and the Aquarium sell at CatchRules.SalePrice.
        var (game, clock, _) = TestSupport.NewGame(TestSupport.CertainCatchConfig());
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 120, stepSeconds: 10);
        var box = game.Fishing.GetFishingBox();
        Assert.NotEmpty(box);
        var expected = box.Sum(c => c.SalePriceCoins);
        var before = game.Player.GetPlayer().Coins;

        var sold = game.Fishing.SellCatches(box.Select(c => c.CatchId).ToList());

        Assert.True(sold.Succeeded, sold.ErrorMessage);
        Assert.Equal(before + expected, game.Player.GetPlayer().Coins);
    }
}
