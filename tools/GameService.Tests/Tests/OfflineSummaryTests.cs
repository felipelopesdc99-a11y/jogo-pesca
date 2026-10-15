using System.Linq;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Core;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>
/// M24-T51 (A-175): the "Bem-vindo de volta" summary shows numbers the service works out when the offline report is made:
/// the sale value of the catches, the Fisher's levels, the Fishing Box, the catches by rarity and the Fragmentos de Roupa.
/// </summary>
public sealed class OfflineSummaryTests
{
    private static FishingIdle.GameService.Fishing.OfflineReport ThreeHoursAway(GameConfig config, out LocalGame reopened, out int levelBefore)
    {
        var saveDir = TestSupport.NewTempDirectory();
        var clock = new ManualClock(TestSupport.StartMs);
        var (game, _, _) = TestSupport.NewGame(config, saveDir: saveDir, clock: clock);
        game.Fishing.StartFishing();
        game.Fishing.MarkSeen();
        levelBefore = game.Player.GetPlayer().FisherLevel;

        clock.AdvanceSeconds(3 * 3600);
        (reopened, _, _) = TestSupport.NewGame(config, saveDir: saveDir, clock: clock);
        return reopened.Fishing.TakeOfflineReport();
    }

    [Fact]
    public void The_value_of_the_offline_catches_is_the_sum_of_their_sale_prices()
    {
        var report = ThreeHoursAway(TestSupport.RealConfig(), out var game, out _);
        var catches = report.Update.NewCatches;
        Assert.NotEmpty(catches);

        Assert.Equal(catches.Sum(c => c.SalePriceCoins), report.SaleValueCoins);
        // The same as selling them to the NPC would pay.
        Assert.Equal(game.Fishing.PreviewSale(catches.Select(c => c.CatchId).ToList()).TotalCoins, report.SaleValueCoins);
    }

    [Fact]
    public void The_report_tells_the_fisher_level_before_and_after()
    {
        var report = ThreeHoursAway(TestSupport.RealConfig(), out var game, out var levelBefore);

        Assert.Equal(levelBefore, report.FisherLevelBefore);
        Assert.Equal(game.Player.GetPlayer().FisherLevel, report.FisherLevelAfter);
        Assert.True(report.FisherLevelAfter >= report.FisherLevelBefore);
        if (report.Update.LevelsReached.Count > 0)
        {
            Assert.Equal(report.Update.LevelsReached.Max(), report.FisherLevelAfter);
        }
    }

    [Fact]
    public void The_catches_by_rarity_add_up_and_follow_the_tier_order()
    {
        var config = TestSupport.RealConfig();
        var report = ThreeHoursAway(config, out _, out _);

        Assert.Equal(report.Update.NewCatches.Count, report.Rarities.Sum(r => r.Count));
        Assert.All(report.Rarities, r => Assert.True(r.Count > 0));
        var order = config.Progression.Rarity.Tiers.Select(t => t.Id).ToList();
        var indexes = report.Rarities.Select(r => order.IndexOf(r.RarityId)).ToList();
        Assert.Equal(indexes.OrderBy(i => i).ToList(), indexes);
        foreach (var r in report.Rarities)
        {
            Assert.Equal(report.Update.NewCatches.Count(c => c.RarityId == r.RarityId), r.Count);
        }
    }

    [Fact]
    public void The_report_tells_how_full_the_fishing_box_is()
    {
        var report = ThreeHoursAway(TestSupport.RealConfig(), out var game, out _);

        Assert.Equal(game.Fishing.GetFishingBox().Count, report.FishingBoxCount);
        Assert.Equal(8640, report.FishingBoxCapacity);
        Assert.False(report.FishingBoxFull);
    }

    [Fact]
    public void A_box_filled_while_away_is_reported_full()
    {
        var small = TestSupport.ConfigWith(GameConfigLoader.EconomyFile, j => j.Replace("\"capacity\": 8640", "\"capacity\": 3"), certainCatch: true).Config;
        var report = ThreeHoursAway(small, out _, out _);

        Assert.Equal(3, report.FishingBoxCount);
        Assert.Equal(3, report.FishingBoxCapacity);
        Assert.True(report.FishingBoxFull);
        Assert.True(report.Update.SkippedBoxFull > 0);
    }

    [Fact]
    public void The_report_tells_the_clothing_fragments_and_the_next_piece_cost()
    {
        var config = TestSupport.RealConfig();
        var report = ThreeHoursAway(config, out _, out _);

        // No piece assembled yet: the next one is the first.
        Assert.Equal(config.ClothingAssembleCost(0), report.ClothingFragmentsNeed);
        Assert.Equal(report.Update.ClothingFragments, report.ClothingFragmentsHave);
    }
}
