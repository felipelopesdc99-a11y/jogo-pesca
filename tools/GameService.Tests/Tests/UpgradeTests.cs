using FishingIdle.GameService.Aquarium;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Crew;
using FishingIdle.GameService.Fishing;
using FishingIdle.GameService.Market;
using FishingIdle.GameService.Persistence;
using FishingIdle.GameService.Upgrades;
using FishingIdle.Texts;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>
/// Upgrades bought with Moedas (M24-T06, A-155, TD-040): the Crew members' ×2s (unlocked by quantity, bought once),
/// the general upgrades with levels, their effects on the Crew's Moedas and XP, the fish sale and the offline cap, no
/// Fisher level needed, no overflow, and the save migration v13 → v14.
/// </summary>
public sealed class UpgradeTests
{
    private static GameConfig Config => TestSupport.RealConfig();

    private static string MemberId(GameConfig config, int index) => config.Crew.Members[index].Id;

    private static GeneralUpgradeConfig General(GameConfig config, string effect) => config.Upgrades.GeneralUpgrades.Single(u => u.Effect == effect);

    private static double Rate(LocalGame game) => CrewRules.CoinsPerSecond(game.Session.Config, game.Session.Save.Crew, game.Session.Save.Upgrades);

    // ------------------------------------------------------------------ config

    [Fact]
    public void The_upgrades_file_has_five_crew_tiers_and_five_general_upgrades_in_portuguese()
    {
        var config = Config;
        var crew = config.Upgrades.CrewUpgrades;
        Assert.Equal(2.0, crew.Multiplier);
        Assert.Equal(new[] { 1, 10, 25, 50, 100 }, crew.Tiers.Select(t => t.UnlockCount));
        Assert.All(config.Crew.Members, m => Assert.False(string.IsNullOrWhiteSpace(crew.MemberSuffixes[m.Id])));

        Assert.Equal(new[] { "Rádio do Porto", "Freguesia na Feira", "Caixa Térmica", "Sonar de Cardume", "Maré Boa" },
            config.Upgrades.GeneralUpgrades.Select(u => u.DisplayName));
        Assert.Equal(UpgradeEffects.All.OrderBy(e => e), config.Upgrades.GeneralUpgrades.Select(u => u.Effect).OrderBy(e => e));
        Assert.All(config.Upgrades.GeneralUpgrades, u => Assert.InRange(u.MaxLevel, 5, 10));
        Assert.Equal(10.0, General(config, UpgradeEffects.CrewOfflineHours).ValuePerLevel * General(config, UpgradeEffects.CrewOfflineHours).MaxLevel);
    }

    [Fact]
    public void An_unknown_effect_a_missing_suffix_and_tiers_out_of_order_are_refused()
    {
        var effect = TestSupport.ConfigWith(GameConfigLoader.UpgradesFile, j => j.Replace("\"effect\": \"crew_xp\"", "\"effect\": \"magic\""));
        Assert.False(effect.Succeeded);
        Assert.Contains(effect.Errors, e => e.Contains("upgrades.json") && e.Contains("magic"));

        var suffix = TestSupport.ConfigWith(GameConfigLoader.UpgradesFile, j =>
        {
            var root = JObject.Parse(j);
            ((JObject)root["crew_upgrades"]!["member_suffixes"]!).Remove("crew_05");
            return root.ToString();
        });
        Assert.False(suffix.Succeeded);
        Assert.Contains(suffix.Errors, e => e.Contains("crew_05"));

        var tiers = TestSupport.ConfigWith(GameConfigLoader.UpgradesFile, j =>
        {
            var root = JObject.Parse(j);
            root["crew_upgrades"]!["tiers"]![1]!["unlock_count"] = 1;
            return root.ToString();
        });
        Assert.False(tiers.Succeeded);
    }

    [Fact]
    public void A_general_id_that_clashes_with_a_crew_upgrade_id_is_refused()
    {
        var result = TestSupport.ConfigWith(GameConfigLoader.UpgradesFile, j => j.Replace("\"id\": \"port_radio\"", "\"id\": \"crew_01_up_1\""));
        Assert.False(result.Succeeded);
    }

    [Fact]
    public void Crew_upgrade_ids_and_names_are_built_from_the_member_and_the_tier()
    {
        var config = Config;
        var canoeiro = config.Crew.Members[1];
        Assert.Equal("crew_02_up_3", UpgradeRules.CrewUpgradeId("crew_02", 2));
        Assert.True(UpgradeRules.TryGetCrewUpgrade(config, "crew_02_up_3", out var member, out var tier));
        Assert.Same(canoeiro, member);
        Assert.Equal(2, tier);
        Assert.False(UpgradeRules.TryGetCrewUpgrade(config, "crew_02_up_9", out _, out _));
        Assert.False(UpgradeRules.TryGetCrewUpgrade(config, "port_radio", out _, out _));
        Assert.Equal("Anzol Afiado do Canoeiro", UpgradeRules.CrewUpgradeName(config, canoeiro, 0));
        Assert.Equal("Rede Nova da Traineira", UpgradeRules.CrewUpgradeName(config, config.Crew.Members[7], 2));
    }

    // ------------------------------------------------------------------ Crew members' upgrades

    [Fact]
    public void A_crew_upgrade_unlocks_with_the_members_quantity_and_costs_a_multiple_of_its_unit_price()
    {
        var (game, _, _) = TestSupport.NewGame();
        var config = game.Session.Config;
        var save = game.Session.Save;
        save.Coins = 1_000_000_000;
        var first = config.Crew.Members[0];
        var id = UpgradeRules.CrewUpgradeId(first.Id, 0);

        // Nobody hired: the upgrade is shown, locked, and refused.
        var view = game.Upgrades.GetUpgrades();
        var offer = view.NextCrew.Single(o => o.UpgradeId == id);
        Assert.Equal(ServiceError.UpgradeLocked, offer.BuyBlocker);
        Assert.Equal(1, offer.UnlockCount);
        Assert.Equal(ServiceError.UpgradeLocked, game.Upgrades.Buy(id).Error);
        Assert.Equal(1_000_000_000, save.Coins);

        // One unit: on sale for cost_factor × base_cost × cost_growth^1.
        Assert.True(game.Crew.Hire(first.Id, CrewBuyMode.One).Succeeded);
        var tier = config.Upgrades.CrewUpgrades.Tiers[0];
        var expected = (long)Math.Round(tier.CostFactor * first.BaseCost * Math.Pow(first.CostGrowth, tier.UnlockCount), MidpointRounding.AwayFromZero);
        Assert.Equal(expected, UpgradeRules.CrewUpgradeCost(config, first, 0));
        Assert.Equal(ServiceError.None, game.Upgrades.GetUpgrades().NextCrew.Single(o => o.UpgradeId == id).BuyBlocker);

        var coins = save.Coins;
        var bought = game.Upgrades.Buy(id);
        Assert.True(bought.Succeeded);
        Assert.Equal(expected, bought.Value.CoinsSpent);
        Assert.Equal(coins - expected, save.Coins);
        Assert.Equal("Anzol Afiado do Ajudante da Isca", bought.Value.Name);

        // The second tier still waits for 10 units.
        Assert.Equal(ServiceError.UpgradeLocked, game.Upgrades.Buy(UpgradeRules.CrewUpgradeId(first.Id, 1)).Error);
    }

    [Fact]
    public void A_crew_upgrade_is_bought_only_once()
    {
        var (game, _, _) = TestSupport.NewGame();
        var save = game.Session.Save;
        save.Coins = 1_000_000_000;
        var first = MemberId(game.Session.Config, 0);
        save.Crew.Units[first] = 5;
        var id = UpgradeRules.CrewUpgradeId(first, 0);

        Assert.True(game.Upgrades.Buy(id).Succeeded);
        var coins = save.Coins;
        Assert.Equal(ServiceError.UpgradeAlreadyBought, game.Upgrades.Buy(id).Error);
        Assert.Equal(coins, save.Coins);
        Assert.Equal(1, save.Upgrades.LevelOf(id));

        var view = game.Upgrades.GetUpgrades();
        Assert.DoesNotContain(view.NextCrew, o => o.UpgradeId == id);
        Assert.Contains(view.Bought, o => o.UpgradeId == id && o.Level == 1);
        Assert.Equal(1, view.CrewBoughtCount);
        Assert.Equal(game.Session.Config.Crew.Members.Count * 5, view.CrewTotalCount);
        Assert.Equal(ServiceError.UpgradeNotFound, game.Upgrades.Buy("nada").Error);
    }

    [Fact]
    public void Each_crew_upgrade_doubles_only_that_members_coins_not_its_xp()
    {
        var (game, _, _) = TestSupport.NewGame();
        var config = game.Session.Config;
        var save = game.Session.Save;
        save.Coins = long.MaxValue / 4;
        var tarrafeiro = config.Crew.Members[2];
        var jangadeiro = config.Crew.Members[3];
        save.Crew.Units[tarrafeiro.Id] = 30;
        save.Crew.Units[jangadeiro.Id] = 30;
        var fleet = CrewRules.FleetMultiplier(config, save.Crew);
        var before = Rate(game);
        var xpBefore = CrewRules.XpPerSecond(config, save.Crew, save.Upgrades);
        var own = CrewRules.MemberCoinsPerSecond(config, tarrafeiro, 30, fleet);

        Assert.True(game.Upgrades.Buy(UpgradeRules.CrewUpgradeId(tarrafeiro.Id, 0)).Succeeded);
        Assert.True(game.Upgrades.Buy(UpgradeRules.CrewUpgradeId(tarrafeiro.Id, 1)).Succeeded);
        Assert.True(game.Upgrades.Buy(UpgradeRules.CrewUpgradeId(tarrafeiro.Id, 2)).Succeeded);

        // ×8 on the Tarrafeiro (three ×2s); the Jangadeiro is untouched; the XP too.
        Assert.Equal(before + own * 7, Rate(game), 6);
        Assert.Equal(8.0, UpgradeRules.CrewMemberMultiplier(config, save.Upgrades, tarrafeiro.Id));
        Assert.Equal(1.0, UpgradeRules.CrewMemberMultiplier(config, save.Upgrades, jangadeiro.Id));
        Assert.Equal(xpBefore, CrewRules.XpPerSecond(config, save.Crew, save.Upgrades), 9);

        // The Crew window shows it in the member's own multiplier (×2 for the 25 milestone × 8).
        var row = game.Crew.GetCrew(CrewBuyMode.One).Members[2];
        Assert.Equal(16.0, row.Multiplier);
        Assert.Equal(own * 8, row.CoinsPerSecond, 6);
    }

    [Fact]
    public void The_window_lists_at_most_three_crew_upgrades_the_cheapest_first()
    {
        var (game, _, _) = TestSupport.NewGame();
        var save = game.Session.Save;
        foreach (var m in game.Session.Config.Crew.Members)
        {
            save.Crew.Units[m.Id] = 120;
        }

        var view = game.Upgrades.GetUpgrades();
        Assert.Equal(LocalUpgradeService.CrewOffersShown, view.NextCrew.Count);
        Assert.Equal(view.NextCrew.Select(o => o.Cost).OrderBy(c => c), view.NextCrew.Select(o => o.Cost));
        Assert.All(view.NextCrew, o => Assert.NotEqual(ServiceError.UpgradeLocked, o.BuyBlocker));
        Assert.Equal(game.Session.Config.Crew.Members.Count * 5 - 3, view.CrewAvailableCount);
        Assert.Equal(game.Session.Config.Upgrades.GeneralUpgrades.Count, view.General.Count);
    }

    // ------------------------------------------------------------------ general upgrades

    [Fact]
    public void General_upgrades_go_up_level_by_level_with_growing_prices_until_the_max()
    {
        var (game, _, _) = TestSupport.NewGame();
        var save = game.Session.Save;
        save.Coins = long.MaxValue / 2;
        var radio = General(game.Session.Config, UpgradeEffects.CrewCoins);

        long previous = 0;
        for (var level = 0; level < radio.MaxLevel; level++)
        {
            var expected = (long)Math.Round(radio.BaseCost * Math.Pow(radio.CostGrowth, level), MidpointRounding.AwayFromZero);
            Assert.Equal(expected, game.Upgrades.GetUpgrades().General.Single(o => o.UpgradeId == radio.Id).Cost);
            var coins = save.Coins;
            var result = game.Upgrades.Buy(radio.Id);
            Assert.True(result.Succeeded);
            Assert.Equal(level + 1, result.Value.Level);
            Assert.Equal(expected, coins - save.Coins);
            Assert.True(expected > previous);
            previous = expected;
        }

        var maxed = game.Upgrades.GetUpgrades().General.Single(o => o.UpgradeId == radio.Id);
        Assert.Equal(ServiceError.UpgradeMaxLevel, maxed.BuyBlocker);
        Assert.Equal(UpgradeRules.Unaffordable, maxed.Cost);
        Assert.Equal(radio.MaxLevel * radio.ValuePerLevel, maxed.EffectTotal, 9);
        var before = save.Coins;
        Assert.Equal(ServiceError.UpgradeMaxLevel, game.Upgrades.Buy(radio.Id).Error);
        Assert.Equal(before, save.Coins);
    }

    [Fact]
    public void Without_the_coins_nothing_is_bought()
    {
        var (game, _, _) = TestSupport.NewGame();
        var save = game.Session.Save;
        var radio = General(game.Session.Config, UpgradeEffects.CrewCoins);
        save.Coins = radio.BaseCost - 1;

        Assert.Equal(ServiceError.NotEnoughCoins, game.Upgrades.GetUpgrades().General.Single(o => o.UpgradeId == radio.Id).BuyBlocker);
        Assert.Equal(ServiceError.NotEnoughCoins, game.Upgrades.Buy(radio.Id).Error);
        Assert.Equal(radio.BaseCost - 1, save.Coins);
        Assert.Equal(0, save.Upgrades.LevelOf(radio.Id));
    }

    [Fact]
    public void The_port_radio_multiplies_the_whole_crews_coins()
    {
        var (game, _, _) = TestSupport.NewGame();
        var config = game.Session.Config;
        var save = game.Session.Save;
        save.Coins = long.MaxValue / 2;
        foreach (var m in config.Crew.Members.Take(5))
        {
            save.Crew.Units[m.Id] = 40;
        }

        var before = Rate(game);
        var radio = General(config, UpgradeEffects.CrewCoins);
        game.Upgrades.Buy(radio.Id);
        game.Upgrades.Buy(radio.Id);

        Assert.Equal(before * (1 + 2 * radio.ValuePerLevel), Rate(game), 6);
        Assert.Equal(Rate(game), game.Crew.GetCrew(CrewBuyMode.One).CoinsPerSecond, 6);
    }

    [Fact]
    public void The_school_sonar_multiplies_the_crews_xp()
    {
        var (game, clock, _) = TestSupport.NewGame();
        var config = game.Session.Config;
        var save = game.Session.Save;
        save.Coins = long.MaxValue / 2;
        save.Crew.Units[MemberId(config, 9)] = 200;
        var sonar = General(config, UpgradeEffects.CrewXp);
        var before = CrewRules.XpPerSecond(config, save.Crew, save.Upgrades);
        for (var i = 0; i < 3; i++)
        {
            Assert.True(game.Upgrades.Buy(sonar.Id).Succeeded);
        }

        var rate = CrewRules.XpPerSecond(config, save.Crew, save.Upgrades);
        Assert.Equal(before * (1 + 3 * sonar.ValuePerLevel), rate, 9);

        game.Crew.Sync();
        var xp = save.FisherXpTotal;
        for (var i = 0; i < 100; i++)
        {
            clock.AdvanceSeconds(1);
            game.Crew.Sync();
        }

        Assert.InRange(save.FisherXpTotal - xp, (long)(rate * 100) - 1, (long)(rate * 100) + 1);
    }

    [Fact]
    public void The_cooler_box_raises_the_crews_offline_cap()
    {
        var config = Config;
        var cooler = General(config, UpgradeEffects.CrewOfflineHours);
        var (game, clock, dir) = TestSupport.NewGame();
        var save = game.Session.Save;
        save.Coins = long.MaxValue / 2;
        save.Crew.Units[MemberId(config, 4)] = 60;
        Assert.True(game.Upgrades.Buy(cooler.Id).Succeeded);
        Assert.True(game.Upgrades.Buy(cooler.Id).Succeeded);
        game.Crew.MarkSeen();
        var rate = Rate(game);
        var coins = save.Coins;
        var cap = config.Crew.Offline.MaxHours + 2 * cooler.ValuePerLevel;
        Assert.Equal(cap, game.Crew.GetCrew(CrewBuyMode.One).OfflineMaxHours);

        clock.AdvanceSeconds((cap + 6) * 3600);
        var reopened = TestSupport.NewGame(saveDir: dir, clock: clock).Game;

        var full = config.Crew.Offline.FullRateHours;
        var effective = (full + (cap - full) * config.Crew.Offline.ReducedRate) * 3600;
        var report = reopened.Crew.TakeOfflineReport();
        Assert.Equal(cap, report.MaxHours);
        Assert.True(report.Capped);
        Assert.InRange(reopened.Session.Save.Coins - coins, (long)(rate * effective) - 1, (long)(rate * effective) + 1);

        Assert.Equal(cap * 3600, CrewRules.OfflineSeconds(config.Crew, (long)(cap * 3600 * 1000), 2 * cooler.ValuePerLevel).FullSeconds
            + CrewRules.OfflineSeconds(config.Crew, (long)(cap * 3600 * 1000), 2 * cooler.ValuePerLevel).ReducedSeconds, 3);
    }

    [Fact]
    public void Buying_credits_the_crews_income_first_at_the_old_rate()
    {
        var (game, clock, _) = TestSupport.NewGame();
        var config = game.Session.Config;
        var save = game.Session.Save;
        save.Crew.Units[MemberId(config, 3)] = 30;
        save.Coins = 10_000_000;
        game.Crew.Sync();
        var rate = Rate(game);
        var coins = save.Coins;
        var radio = General(config, UpgradeEffects.CrewCoins);

        clock.AdvanceSeconds(60);
        var bought = game.Upgrades.Buy(radio.Id);

        Assert.True(bought.Succeeded);
        Assert.InRange(save.Coins - (coins - radio.BaseCost), (long)(rate * 60) - 1, (long)(rate * 60) + 1);
        Assert.Equal(Rate(game), bought.Value.CrewCoinsPerSecond, 6);
    }

    // ------------------------------------------------------------------ fishing

    [Fact]
    public void Freguesia_and_mare_boa_raise_the_fishing_box_sale_and_freguesia_the_aquarium_but_not_the_market()
    {
        var (game, clock, _) = TestSupport.NewGame();
        var config = game.Session.Config;
        var save = game.Session.Save;
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 900, stepSeconds: 30);
        game.Fishing.StopFishing();
        var box = game.Fishing.GetFishingBox();
        Assert.True(box.Count >= 6);
        var basePrices = box.ToDictionary(c => c.CatchId, c => c.SalePriceCoins);

        save.Coins = long.MaxValue / 4;
        var feira = General(config, UpgradeEffects.FishSale);
        var mare = General(config, UpgradeEffects.FishingCoins);
        for (var i = 0; i < 3; i++)
        {
            game.Upgrades.Buy(feira.Id);
        }

        game.Upgrades.Buy(mare.Id);
        var boxMultiplier = (1 + 3 * feira.ValuePerLevel) * (1 + mare.ValuePerLevel);
        Assert.Equal(boxMultiplier, UpgradeRules.BoxSaleMultiplier(config, save.Upgrades), 9);

        // Each card shows the raised price; the sale pays exactly what the cards show.
        var raised = game.Fishing.GetFishingBox();
        foreach (var c in raised)
        {
            var entry = save.FishingBox.Single(e => e.Id == c.CatchId);
            config.TryGetSpecies(entry.SpeciesId, out var species);
            Assert.Equal(CatchRules.SalePrice(config, species, entry.SizeMm, boxMultiplier), c.SalePriceCoins);
            Assert.True(c.SalePriceCoins >= basePrices[c.CatchId]);
        }

        var sell = raised.Take(3).Select(c => c.CatchId).ToList();
        var preview = game.Fishing.PreviewSale(sell);
        var coins = save.Coins;
        var sale = game.Fishing.SellCatches(sell);
        Assert.Equal(preview.TotalCoins, sale.Value.CoinsGained);
        Assert.Equal(raised.Take(3).Sum(c => c.SalePriceCoins), sale.Value.CoinsGained);
        Assert.Equal(coins + sale.Value.CoinsGained, save.Coins);
        Assert.True(sale.Value.CoinsGained > sell.Sum(id => basePrices[id]));

        // In the Aquarium only the Freguesia counts.
        var kept = game.Aquarium.KeepCatches(raised.Skip(3).Take(1).Select(c => c.CatchId).ToList()).Value.Kept[0];
        var fish = save.Aquarium.Single(f => f.Id == kept.FishId);
        config.TryGetSpecies(fish.SpeciesId, out var fishSpecies);
        Assert.Equal(CatchRules.SalePrice(config, fishSpecies, fish.SizeMm, 1 + 3 * feira.ValuePerLevel), kept.SalePriceCoins);
        var before = save.Coins;
        Assert.Equal(kept.SalePriceCoins, game.Aquarium.SellFish(new[] { kept.FishId }).Value.CoinsGained);
        Assert.Equal(before + kept.SalePriceCoins, save.Coins);

        // The Market's reference price (between players) does not change.
        var left = save.FishingBox.First();
        config.TryGetSpecies(left.SpeciesId, out var leftSpecies);
        Assert.Equal(CatchRules.SalePrice(config, leftSpecies, left.SizeMm), MarketRules.NpcValue(config, new MarketGoods { Kind = MarketGoods.KindFish, Fish = new FishInstance { SpeciesId = left.SpeciesId, SizeMm = left.SizeMm, Level = 1 } }));
    }

    // ------------------------------------------------------------------ no level, no overflow

    [Fact]
    public void No_fisher_level_is_needed_to_buy_any_upgrade()
    {
        var (game, _, _) = TestSupport.NewGame();
        var config = game.Session.Config;
        var save = game.Session.Save;
        Assert.Equal(1, save.FisherLevel);
        save.Coins = long.MaxValue / 2;
        foreach (var m in config.Crew.Members)
        {
            save.Crew.Units[m.Id] = 100;
        }

        foreach (var upgrade in config.Upgrades.GeneralUpgrades)
        {
            Assert.True(game.Upgrades.Buy(upgrade.Id).Succeeded, upgrade.Id);
        }

        // The cheap tiers of every member (the dearest ones of the last members do not fit long.MaxValue / 2).
        foreach (var m in config.Crew.Members)
        {
            Assert.True(game.Upgrades.Buy(UpgradeRules.CrewUpgradeId(m.Id, 0)).Succeeded, m.Id);
        }

        Assert.Equal(1, save.FisherLevel);
    }

    [Fact]
    public void Prices_too_big_for_a_number_are_unaffordable_and_huge_incomes_never_wrap()
    {
        var result = TestSupport.ConfigWith(GameConfigLoader.UpgradesFile, j =>
        {
            var root = JObject.Parse(j);
            root["crew_upgrades"]!["tiers"]![4]!["cost_factor"] = 1e12;
            root["general_upgrades"]![0]!["cost_growth"] = 1000.0;
            return root.ToString();
        });
        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        var config = result.Config;
        var last = config.Crew.Members[9];
        Assert.Equal(UpgradeRules.Unaffordable, UpgradeRules.CrewUpgradeCost(config, last, 4));
        Assert.Equal(UpgradeRules.Unaffordable, UpgradeRules.GeneralCost(config.Upgrades.GeneralUpgrades[0], 9));
        Assert.Equal(UpgradeRules.Unaffordable, UpgradeRules.GeneralCost(config.Upgrades.GeneralUpgrades[0], -1));

        var (game, clock, _) = TestSupport.NewGame(config);
        var save = game.Session.Save;
        save.Coins = long.MaxValue;
        foreach (var m in config.Crew.Members)
        {
            save.Crew.Units[m.Id] = 5_000;
        }

        Assert.Equal(ServiceError.NotEnoughCoins, game.Upgrades.Buy(UpgradeRules.CrewUpgradeId(last.Id, 4)).Error);
        Assert.Equal(long.MaxValue, save.Coins);

        // Every upgrade that fits, then an absurd income for a day: the wallet stops at the largest number.
        foreach (var upgrade in config.Upgrades.GeneralUpgrades)
        {
            while (game.Upgrades.Buy(upgrade.Id).Succeeded)
            {
            }
        }

        foreach (var m in config.Crew.Members)
        {
            for (var i = 0; i < config.Upgrades.CrewUpgrades.Tiers.Count; i++)
            {
                game.Upgrades.Buy(UpgradeRules.CrewUpgradeId(m.Id, i));
            }
        }

        game.Crew.Sync();
        clock.AdvanceSeconds(24 * 3600);
        game.Crew.Sync();
        Assert.InRange(save.Coins, 0, long.MaxValue);
        Assert.InRange(save.Crew.CoinsEarned, 0, long.MaxValue);
        Assert.True(Rate(game) > 0 && !double.IsInfinity(Rate(game)));
    }

    // ------------------------------------------------------------------ save

    [Fact]
    public void Bought_upgrades_survive_closing_the_game()
    {
        var (game, clock, dir) = TestSupport.NewGame();
        var save = game.Session.Save;
        save.Coins = 100_000_000;
        save.Crew.Units[MemberId(game.Session.Config, 0)] = 12;
        var radio = General(game.Session.Config, UpgradeEffects.CrewCoins);
        game.Upgrades.Buy(radio.Id);
        game.Upgrades.Buy(UpgradeRules.CrewUpgradeId(MemberId(game.Session.Config, 0), 1));

        var reopened = TestSupport.NewGame(saveDir: dir, clock: clock).Game;
        Assert.Equal(1, reopened.Session.Save.Upgrades.LevelOf(radio.Id));
        Assert.Equal(1, reopened.Session.Save.Upgrades.LevelOf("crew_01_up_2"));
        Assert.Contains("\"upgrades\"", File.ReadAllText(Path.Combine(dir, JsonFilePlayerRepository.SaveFileName)));
    }

    [Fact]
    public void A_version_13_save_starts_with_no_upgrades()
    {
        var (game, clock, dir) = TestSupport.NewGame();
        game.Session.Save.Coins = 7_000;
        game.Session.Save.Crew.Units["crew_01"] = 3;
        game.Session.Persist();

        // The same player as a version 13 file, from before the Upgrades.
        var path = Path.Combine(dir, JsonFilePlayerRepository.SaveFileName);
        var json = JObject.Parse(File.ReadAllText(path));
        json["save_version"] = 13;
        json.Remove("upgrades");
        File.WriteAllText(path, json.ToString());

        var reopened = TestSupport.NewGame(saveDir: dir, clock: clock).Game;
        var save = reopened.Session.Save;

        Assert.True(PlayerSave.CurrentVersion >= 14);
        Assert.Equal(PlayerSave.CurrentVersion, save.SaveVersion);
        Assert.NotNull(save.Upgrades);
        Assert.Empty(save.Upgrades.Levels);
        Assert.Equal(3, save.Crew.UnitsOf("crew_01"));
        Assert.Equal(7_000, save.Coins);
        Assert.Contains("\"save_version\": " + PlayerSave.CurrentVersion, File.ReadAllText(path));
    }

    [Fact]
    public void A_save_with_negative_upgrade_levels_is_not_trusted()
    {
        var save = new PlayerSave { PlayerId = "x", CurrentMapId = "map_01" };
        save.Upgrades.Levels["port_radio"] = -1;
        Assert.Contains("upgrades have malformed levels", SaveValidator.Validate(save));
    }

    [Fact]
    public void A_level_above_the_files_max_counts_only_up_to_the_max()
    {
        var config = Config;
        var radio = General(config, UpgradeEffects.CrewCoins);
        var state = new UpgradesState();
        state.Levels[radio.Id] = radio.MaxLevel + 50;
        state.Levels["an_old_upgrade"] = 3;
        Assert.Equal(radio.MaxLevel, UpgradeRules.GeneralLevel(radio, state));
        Assert.Equal(1 + radio.MaxLevel * radio.ValuePerLevel, UpgradeRules.CrewCoinsMultiplier(config, state), 9);
        Assert.Equal(1.0, UpgradeRules.CrewCoinsMultiplier(config, null));
    }

    // ------------------------------------------------------------------ texts

    [Theory]
    [InlineData("crew_member", 2.0, "Canoeiro", "×2 Canoeiro")]
    [InlineData("crew_coins", 0.25, null, "+25% Tripulação")]
    [InlineData("fish_sale", 0.1, null, "+10% na venda de peixe")]
    [InlineData("crew_offline_hours", 2.0, null, "+2 h fora do jogo")]
    [InlineData("crew_xp", 0.05, null, "+5% XP da Tripulação")]
    [InlineData("fishing_coins", 0.125, null, "+12,5% Moedas da pesca")]
    public void Effects_read_in_brazilian_portuguese(string key, double value, string member, string expected)
    {
        Assert.Equal(expected, GameTexts.Upgrades.Effect(key, value, member));
    }
}
