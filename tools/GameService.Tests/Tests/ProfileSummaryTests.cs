using System.Linq;
using Xunit;

namespace FishingIdle.GameService.Tests;

/// <summary>A-121: the own Profile counts species and catches per rarity.</summary>
public sealed class ProfileSummaryTests
{
    [Fact]
    public void Every_rarity_is_tallied_and_the_totals_match()
    {
        var (game, clock, _) = TestSupport.NewGame();
        game.Fishing.StartFishing();
        TestSupport.PlayFor(game, clock, 600, stepSeconds: 30);

        var records = game.Profile.GetProfile().Records;
        Assert.Equal(5, records.ByRarity.Count);
        Assert.Equal("common", records.ByRarity[0].RarityId);
        Assert.Equal(records.SpeciesTotal, records.ByRarity.Sum(t => t.SpeciesTotal));
        Assert.Equal(records.SpeciesDiscovered, records.ByRarity.Sum(t => t.SpeciesFound));
        Assert.Equal(records.TotalCatches, records.ByRarity.Sum(t => t.Caught));
    }
}
